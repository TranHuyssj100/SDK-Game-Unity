# LSDK Spine Anim  (`LSDK.SpineAnim`)

Hỗ trợ **Spine 4.3** (spine-unity): mix skin, chạy/mix animation, sequence animation.

## Yêu cầu
- Unity 2021.3+
- Spine runtime 4.3 (spine-csharp + spine-unity) đã có trong project.
  Asmdef đang reference theo tên `spine-csharp` và `spine-unity`. Nếu bản Spine của m đặt tên khác → sửa `references` trong 2 file `.asmdef`.

## Cài
Package Manager → Add package from disk… → chọn `package.json`
(hoặc Add package from tarball… → `.tgz`, hoặc git URL `...?path=/com.lsdk.spineanim`).

## Dùng
1. Object có `SkeletonAnimation` hoặc `SkeletonGraphic` → Add Component **LSDK/Spine/Spine Anim Controller**.
2. Inspector tự liệt kê toàn bộ skin + animation.
3. **Skins**: tick nhiều skin → tự gộp thành 1 skin combined (skin chọn sau đè skin trước).
4. **Animations**: bấm ▶ để chạy ngay (cả ở edit mode). Track khác nhau + Alpha = mix nhiều animation chồng nhau.
5. **Sequences**: tạo chuỗi animation (Loop/Mix/Speed từng bước), bấm Play để xem.

## Code
```csharp
using LSDK.SpineAnim;

var c = GetComponent<SpineAnimController>();
c.SetSkins(new[] { "hat/red", "weapon/sword" });   // mix skin
c.Play("run", loop: true, track: 0);
c.Play("aim", loop: true, track: 1, alpha: 0.7f);  // mix layer
c.SequenceStepStarted += (seq, i) => Debug.Log($"{seq.name} step {i}");
c.SequenceCompleted   += seq => Debug.Log("done " + seq.name);
c.PlaySequence("Intro");
```
