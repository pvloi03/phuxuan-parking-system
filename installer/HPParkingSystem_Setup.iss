; ==============================================================================
; Script Inno Setup: Đóng gói Bộ cài đặt HPPARKING (x86 Client)
; ==============================================================================

#define MyAppName "HPPARKING"
#define MyAppFullName "Hệ Thống Kiểm Soát Bãi Xe HPPARKING"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Phú Xuân Parking System"
#define MyAppExeName "HPParkingSystem.exe"

[Setup]
; Mã AppId GUID duy nhất định danh ứng dụng
AppId={{C78E1234-9A4B-4E89-BCDF-890123456789}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\publish\client
OutputBaseFilename={#MyAppName}_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesInstallIn64BitMode=
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
; Tự động cập nhật đè khi cài lại (không hiện thông báo hỏi thư mục đã tồn tại)
DirExistsWarning=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng ngoài màn hình Desktop ({#MyAppName})"; GroupDescription: "Tùy chọn bổ sung:"
Name: "autostart"; Description: "Tự động khởi động cùng Windows khi bật máy"; GroupDescription: "Tùy chọn bổ sung:"

[Files]
; Đóng gói toàn bộ các file trong thư mục Release của dự án (gồm exe, dll, sdk native, config, thư mục con HCNetSDKCom)
Source: "..\HPParkingSystem\bin\x86\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Biểu tượng trong Start Menu và Desktop (WorkingDir={app} cực kỳ quan trọng để load đúng DLL Native)
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Gỡ cài đặt {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
; Đăng ký khởi động cùng Windows nếu người dùng tích chọn lúc cài đặt (HKCU không cần quyền Admin)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
; Tùy chọn khởi chạy ứng dụng ngay sau khi cài xong
Description: "Khởi chạy {#MyAppName} ngay bây giờ"; Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent

[Messages]
; ==============================================================================
; Việt hóa toàn bộ giao diện cài đặt (Inno Setup Unicode Messages)
; ==============================================================================

; --- Các nút bấm điều hướng ---
ButtonBack=< &Quay lại
ButtonNext=&Tiếp tục >
ButtonInstall=&Cài đặt
ButtonOK=Đồng ý
ButtonCancel=Hủy bỏ
ButtonYes=&Có
ButtonNo=&Không
ButtonFinish=&Hoàn tất
ButtonBrowse=&Duyệt...

; --- Tiêu đề cửa sổ & ứng dụng ---
SetupAppTitle=Cài đặt {#MyAppName}
SetupWindowTitle=Cài đặt - {#MyAppName}
UninstallAppTitle=Gỡ cài đặt {#MyAppName}
UninstallAppFullTitle=Gỡ cài đặt {#MyAppName}

; --- Màn hình chọn thư mục cài đặt ---
WizardSelectDir=Chọn thư mục cài đặt
SelectDirDesc=Bạn muốn cài đặt [name] vào thư mục nào?
SelectDirLabel3=Bộ cài đặt sẽ cài đặt [name] vào thư mục sau:
SelectDirBrowseLabel=Để tiếp tục, bấm Tiếp tục. Nếu bạn muốn chọn thư mục khác, bấm Duyệt.
DiskSpaceGBLabel=Cần ít nhất [gb] GB dung lượng đĩa trống.
DiskSpaceMBLabel=Cần ít nhất [mb] MB dung lượng đĩa trống.
DirExistsTitle=Thư mục đã tồn tại
DirExists=Thư mục:%n%n%1%n%nđã tồn tại. Bạn có muốn tiếp tục cài đặt vào thư mục này không?

; --- Màn hình chọn tác vụ (Tasks) ---
WizardSelectTasks=Tùy chọn bổ sung
SelectTasksDesc=Bạn muốn thực hiện những tùy chọn nào?
SelectTasksLabel2=Chọn các tùy chọn bổ sung bạn muốn áp dụng khi cài đặt [name], sau đó bấm Tiếp tục.

; --- Màn hình sẵn sàng cài đặt ---
WizardReady=Sẵn sàng cài đặt
ReadyLabel1=Bộ cài đặt đã sẵn sàng cài đặt [name] trên máy tính của bạn.
ReadyLabel2a=Bấm Cài đặt để bắt đầu, hoặc bấm Quay lại nếu bạn muốn xem lại các tùy chọn.

; --- Màn hình tiến trình cài đặt ---
WizardInstalling=Đang cài đặt
InstallingLabel=Vui lòng đợi trong khi chương trình cài đặt [name] vào máy tính của bạn.
StatusExtractFiles=Đang trích xuất tập tin...
StatusCreateIcons=Đang tạo biểu tượng...
StatusCreateRegistryEntries=Đang thiết lập cấu hình khởi động...
StatusRunProgram=Đang hoàn tất cài đặt...

; --- Màn hình hoàn tất cài đặt ---
FinishedHeadingLabel=Cài đặt [name] hoàn tất
FinishedLabel=Chương trình cài đặt đã hoàn tất cài đặt [name] trên máy tính của bạn.
ClickFinish=Bấm Hoàn tất để kết thúc và đóng trình cài đặt.

; --- Thông báo xác nhận thoát ---
ExitSetupTitle=Thoát cài đặt
ExitSetupMessage=Quá trình cài đặt chưa hoàn tất. Nếu thoát bây giờ, phần mềm sẽ không được cài đặt.%n%nBạn có chắc chắn muốn thoát cài đặt không?
ConfirmTitle=Xác nhận
ErrorTitle=Lỗi
InformationTitle=Thông báo

; --- Thông báo gỡ cài đặt (Uninstaller) ---
ConfirmUninstall=Bạn có chắc chắn muốn gỡ bỏ hoàn toàn %1 và tất cả các thành phần của phần mềm không?
UninstallStatusLabel=Vui lòng đợi trong khi %1 đang được gỡ bỏ khỏi máy tính của bạn.
StatusUninstalling=Đang gỡ cài đặt %1...
UninstalledAll=Đã gỡ bỏ thành công %1 khỏi máy tính của bạn.
UninstalledMost=Quá trình gỡ cài đặt %1 đã hoàn tất.%n%nMột số tập tin không thể xóa tự động, bạn có thể xóa thủ công nếu muốn.
UninstallAppRunningError=Trình gỡ cài đặt phát hiện %1 hiện đang chạy.%n%nVui lòng tắt phần mềm trước, sau đó bấm Đồng ý để tiếp tục, hoặc bấm Hủy bỏ để thoát.

[Code]
var
  DeleteAllData: Boolean;

// Hỏi người dùng khi bắt đầu gỡ bỏ
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    if MsgBox('Bạn có muốn xóa toàn bộ dữ liệu cục bộ (file cấu hình, nhật ký log, dữ liệu tạm) và làm sạch hoàn toàn thư mục cài đặt không?', mbConfirmation, MB_YESNO) = IDYES then
      DeleteAllData := True
    else
      DeleteAllData := False;
  end;

  // Sau khi Inno Setup gỡ xong các file chính, nếu người dùng chọn Xóa thì dọn dẹp sạch toàn bộ thư mục app
  if CurUninstallStep = usPostUninstall then
  begin
    if DeleteAllData then
    begin
      DelTree(ExpandConstant('{app}'), True, True, True);
    end;
  end;
end;
