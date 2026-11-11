namespace LSDK.IAP
{
    /// <summary>
    /// Game tu implement: cap vang, tat ads, mo item...
    /// Neu Grant nem exception thi giao dich KHONG duoc confirm, store se gui lai lan sau.
    /// </summary>
    public interface IRewardGranter
    {
        void Grant(IAPProductDefinition product);
    }
}
