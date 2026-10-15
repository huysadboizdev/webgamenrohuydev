# Tàn sát dùng toàn bộ skill Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Khi tàn sát quái hoặc người, client luân phiên dùng mọi skill nhân vật đã học, trừ tự sát, Quả cầu kinh khí, Makankosappo và hồi sinh; chỉ ẩn animation skill của nhân vật mình.

**Architecture:** Đồng bộ cùng hành vi trong hai source Android/APK và Web/PC, cho cả Game1/Game2. Bộ chọn lấy skill đã học, áp điều kiện cooldown/mana và danh sách loại trừ; bộ thi triển phân nhánh theo loại skill. Chặn phần vẽ skill cục bộ của nhân vật khi tàn sát nhưng giữ HUD, trạng thái/cooldown, quái và hiệu ứng môi trường.

**Tech Stack:** Unity C#, Python regression harness cho source/hành vi C# trích xuất, Unity Editor regression nếu phù hợp.

---

### Task 1: Khóa hành vi chọn skill bằng regression test

**Files:**
- Tạo `Tests/MassacreSkillChecks.py` trong Web project; test method C# được trích từ `PickMobController.cs` ở bốn cặp Android/Web và Game1/Game2.
- Xác nhận source APK dùng chung harness bằng biến đường dẫn repo hoặc đối số CLI.

- [x] Thêm test đỏ cho lọc bốn skill cấm, skill học được và đủ mana/cooldown, luân phiên ứng viên thay vì chỉ chọn một nhóm hard-code; `python Tests/MassacreSkillChecks.py` đã thất bại trước helper và qua sau khi thêm.
- [x] Thêm regression cho dispatch buff, tự dùng, đánh mob và skill đặc biệt có mục tiêu; buff nhắm nhân vật mình, skill đánh dùng mục tiêu đang tàn sát.
- [x] Thêm regression cho guard animation: chỉ áp dụng với nhân vật mình khi một trong hai chế độ tàn sát bật; giữ tiến độ frame, thân nhân vật/HUD, mob và hiệu ứng mục tiêu.

### Task 2: Chọn toàn bộ skill hợp lệ

**Files:**
- Sửa `Assets/Scripts/Assembly-CSharp/Game1/Mod/XMAP/PickMobController.cs` và Game2 tương ứng trong cả hai repo.
- Chỉ sửa `PickMob.cs` nếu cần bỏ cấu hình danh sách ID cũ mà không làm thay đổi lưu cấu hình.

- [x] Liệt kê skill đã học từ `Char.myCharz().vSkill`; loại ID 7 (hồi sinh), 10 (Quả cầu kênh khí), 11 (Makankosappo), 14 (tự nổ), theo hằng số server `SRC/src/nro/models/skill/Skill.java`.
- [x] Lọc theo cooldown/mana với `CanUseSkill`, giữ chặn máu thấp hiện có cho skill ID 9.
- [x] Duy trì con trỏ vòng qua danh sách skill đủ điều kiện để các loại skill được xét lần lượt.
- [x] Regression compile và chạy các helper chọn skill trong Game1/Game2 ở cả source APK/Web.

### Task 3: Thi triển riêng từng loại skill

**Files:**
- Sửa hai `PickMobController.cs` của Game1/Game2 ở mỗi repo.
- Chỉ sửa `GameScr.cs` hoặc `ModFunc.cs` khi cần một đường thi triển tự động không gắn input người chơi.

- [x] Với skill đánh/special, giữ mục tiêu hiện hành, kiểm tra khoảng cách/vị trí và ghi cooldown khi packet gửi thành công.
- [x] Với buff, gửi mục tiêu là nhân vật mình; không chạy tới mục tiêu đang săn để buff.
- [x] Với skill tự dùng, gọi đường `useSkillNotFocus()` không cần mục tiêu và ghi cooldown.
- [x] Dùng cùng bộ lọc cho tàn sát quái/người; skill đặc biệt đi theo đường attack packet có mục tiêu, không mở UI.
- [x] Chạy regression chọn/dispatch cho bốn source.

### Task 4: Ẩn animation riêng của nhân vật mình

**Files:**
- Sửa `Assets/Scripts/Assembly-CSharp/Game1/Char.cs` và Game2 tương ứng trong cả hai repo.
- Không thay vòng vẽ mob, `EffecMn` toàn cục hoặc `GameScr.paintSelectedSkill`.

- [x] Thêm guard cho skill paint và projectile của nhân vật mình; vẽ thân nhân vật ở frame đứng khi animation skill thường/new đang chạy, vẫn để `GameScr` vẽ mount, mob, hiệu ứng va chạm và HUD.
- [x] Không sửa update/network/cooldown; khi ẩn paint vẫn tăng `indexSkill` để animation/server attack tiếp tục hoàn tất.
- [x] Kiểm tra bật/tắt hai cờ tàn sát độc lập theo Game1/Game2 trong regression helper.

### Task 5: Đồng bộ, kiểm tra và bàn giao

**Files:**
- Các source nêu ở trên trong hai repo; `Tests/MassacreSkillChecks.py`; cập nhật báo cáo kế hoạch sau kiểm chứng.

- [x] Chạy `python Tests/MassacreSkillChecks.py`, `python Tests/TabSwitchChecks.py`, `git diff --check` ở cả repo.
- [x] So sánh bốn source; giới hạn thay đổi APK vào `Game1/Char.cs`, `Game1/Mod/XMAP/PickMobController.cs`, `Game2/Char.cs`, `Game2/Mod/XMAP/PickMobController.cs`, giữ nguyên các thay đổi sẵn có khác.
- [x] Không build APK/JAR/EXE do chưa có yêu cầu build.
- [x] Source regressions and actual WebGL Chromium profiles for PC/Android/iOS/iPad passed on both tabs; no authenticated server session or physical device was available.
