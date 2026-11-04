# ZombieShoot

ZombieShoot là game bắn súng sinh tồn góc nhìn thứ nhất viết bằng C# trên Unity. Người chơi vượt qua năm đợt zombie, thu thập vật phẩm và nâng cấp vũ khí để đánh bại boss cuối trận.

## Chạy dự án

**Yêu cầu** · Git, Unity Hub và Unity Editor **6000.3.11f1**. [Tải đúng phiên bản Unity](https://unity.com/releases/editor/whats-new/6000.3.11f1) · [Phiên bản trong repo](ProjectSettings/ProjectVersion.txt)

```bash
git clone https://github.com/Kenfiz123/PRU213_Zombie.git
cd PRU213_Zombie
```

1. Trong Unity Hub, vào **Projects → Add → Add project from disk**. Chọn thư mục `PRU213_Zombie` chứa `Assets`, `Packages` và `ProjectSettings`.
2. Mở dự án bằng Unity `6000.3.11f1`. Đợi Unity tải package và import tài nguyên ở lần mở đầu tiên.
3. Trong cửa sổ **Project**, mở [Assets/Scenes/SampleScene.unity](Assets/Scenes/SampleScene.unity) rồi nhấn **Play**.
4. Từ menu chính, bắt đầu trận và chọn độ khó.

## Cách chơi

- Chọn một trong bốn độ khó **Dễ**, **Bình thường**, **Khó** và **Asian**. Độ khó thay đổi các yếu tố như số lượng zombie, máu địch, sát thương người chơi và tốc độ xuất hiện của rương.
- Dọn hết zombie để qua đợt tiếp theo. Trận đấu có ba giai đoạn gồm **1, 2 và 2 đợt**, với zombie cận chiến, bắn xa và tự nổ. Boss xuất hiện sau đợt thứ năm.
- Mở rương để tìm đạn, vật phẩm hồi máu, giáp hoặc thẻ kỹ năng. Thẻ có thể hồi đầy máu, hồi đầy giáp hoặc triệu hồi đồng đội.
- Dùng điểm kiếm được khi hạ zombie để nâng cấp vũ khí, cải thiện chỉ số nhân vật hoặc mua lựu đạn. Hạ boss để thắng trận.

## Điều khiển

| Phím / thao tác | Hành động |
| --- | --- |
| `W` `A` `S` `D` | Di chuyển |
| Di chuột | Xoay góc nhìn, ngắm |
| `Shift` trái | Chạy nhanh |
| `Space` | Nhảy |
| `C` khi đang di chuyển | Lướt né theo hướng di chuyển |
| Lăn chuột | Đổi giữa pistol, AK và dao |
| Chuột trái | Bắn hoặc chém nhẹ bằng dao |
| Chuột phải | Chém mạnh bằng dao |
| `R` | Nạp đạn |
| `G` | Ném lựu đạn nếu còn trong kho |
| `F` | Bật hoặc tắt đèn pin |
| `E` | Mở rương, nhặt vật phẩm hoặc dùng thẻ đã chọn |
| `1` `2` `3` | Chọn ô thẻ kỹ năng, sau đó nhấn `E` để dùng |
| Giữ `Tab` | Mở túi đồ |
| `B` | Mở bảng nâng cấp |
| `H` | Mở hướng dẫn trong trận |

Khi đứng gần rương hoặc vật phẩm, `E` ưu tiên tương tác với đối tượng đó.

## Build bản chơi

1. Vào **File → Build Profiles** và chọn nền tảng đích, ví dụ **Windows**. Nếu nền tảng yêu cầu thêm module, cài module đó cho phiên bản Editor đang dùng qua Unity Hub.
2. Kiểm tra `Assets/Scenes/SampleScene.unity` đang được bật trong danh sách scene của build. Đây là scene đã bật trong [cấu hình mặc định](ProjectSettings/EditorBuildSettings.asset).
3. Nhấn **Build** hoặc **Build And Run**. Có thể lưu kết quả vào `Builds/`, thư mục đã được Git bỏ qua.

[Tài liệu Build Profiles của Unity 6.3](https://docs.unity.com/en-us/engine/6000.3/manual/building-and-publishing/build-settings/build-profiles)

## Cấu trúc dự án

| Đường dẫn | Nội dung |
| --- | --- |
| [`Assets/Scenes/`](Assets/Scenes/) | Scene chính và dữ liệu NavMesh |
| [`Assets/Code_Off/Player/`](Assets/Code_Off/Player/) | Di chuyển, vũ khí, chỉ số, túi đồ và kỹ năng người chơi |
| [`Assets/Code_Off/Zombie/`](Assets/Code_Off/Zombie/) | Điều phối đợt, AI zombie và boss |
| [`Assets/Code_Off/Chest/`](Assets/Code_Off/Chest/) | Rương, vật phẩm và logic nhặt đồ |
| [`Assets/Code_Off/UI/`](Assets/Code_Off/UI/) | Menu, độ khó, HUD và bảng nâng cấp |
| [`Assets/Code_Off/Environment/`](Assets/Code_Off/Environment/) | Thùng nổ và logic tạo thùng |
| [`Assets/Code_Off/Optimization/`](Assets/Code_Off/Optimization/) | Object pooling, LOD và tiện ích hiệu năng |
| [`Assets/Prefab/`](Assets/Prefab/) | Prefab zombie, boss, rương, vật phẩm và hiệu ứng |
| [`Packages/`](Packages/) và [`ProjectSettings/`](ProjectSettings/) | Package Unity và cấu hình Editor, input, build |

Để chỉnh số đợt, nhóm zombie hoặc vị trí sinh rương, mở `SampleScene` và xem component `WaveManager` trên `GameManager`. Cấu hình input hiện dùng **Both** vì các script sử dụng cả Input System và Input Manager.
