using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LSDK.IAP
{
    /// <summary>
    /// POST IAPReceipt (JSON) len server cua game. Server hoi Google Play Developer API / Apple
    /// roi tra ve:
    /// { "valid": true, "productId": "&lt;SKU store tra ve&gt;", "transactionId": "...", "expiresAtMs": 0, "message": "" }
    /// HTTP != 2xx hoac body hong -> Error (thu lai sau). valid=false -> Invalid.
    /// </summary>
    public class ServerReceiptValidator : IReceiptValidator
    {
        [Serializable]
        private class Response
        {
            public bool valid;
            public string productId;
            public string transactionId;
            public long expiresAtMs;
            public string message;
        }

        private readonly string _url;
        private readonly int _timeoutSeconds;

        /// <summary>Header them vao moi request, vd Authorization / API key.</summary>
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

        /// <summary>Goi truoc moi request de lay header dong (vd access token cua tai khoan game).</summary>
        public Func<IDictionary<string, string>> HeaderProvider;

        public ServerReceiptValidator(string url, int timeoutSeconds = 15)
        {
            _url = url;
            _timeoutSeconds = Mathf.Max(1, timeoutSeconds);
        }

        public void Validate(IAPReceipt receipt, Action<ReceiptValidationResult> done)
        {
            if (string.IsNullOrEmpty(_url))
            {
                done?.Invoke(ReceiptValidationResult.Error("Chua cau hinh URL verify."));
                return;
            }

            var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(receipt));
            var request = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(body),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = _timeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            foreach (var pair in Headers)
                request.SetRequestHeader(pair.Key, pair.Value);

            var dynamicHeaders = HeaderProvider?.Invoke();
            if (dynamicHeaders != null)
            {
                foreach (var pair in dynamicHeaders)
                    request.SetRequestHeader(pair.Key, pair.Value);
            }

            request.SendWebRequest().completed += _ =>
            {
                ReceiptValidationResult result;
                try
                {
                    result = ParseResponse(request);
                }
                finally
                {
                    request.Dispose();
                }
                done?.Invoke(result);
            };
        }

        private static ReceiptValidationResult ParseResponse(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
                return ReceiptValidationResult.Error($"HTTP {request.responseCode}: {request.error}");

            Response response;
            try
            {
                response = JsonUtility.FromJson<Response>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                return ReceiptValidationResult.Error("Body khong phai JSON hop le: " + e.Message);
            }

            if (response == null)
                return ReceiptValidationResult.Error("Body rong.");

            if (!response.valid)
                return ReceiptValidationResult.Invalid(string.IsNullOrEmpty(response.message) ? "Server tu choi don." : response.message);

            long expireTicks = response.expiresAtMs > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(response.expiresAtMs).UtcDateTime.Ticks
                : 0;
            return ReceiptValidationResult.Valid(response.productId, response.transactionId, expireTicks);
        }
    }
}
