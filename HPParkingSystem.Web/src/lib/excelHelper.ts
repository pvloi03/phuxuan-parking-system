import * as XLSX from 'xlsx'
import { notify } from './notify'

/**
 * Xuất dữ liệu ra file Excel (.xlsx) với header đẹp và tự động căn chỉnh độ rộng cột
 */
export function exportToExcel<T extends Record<string, any>>(
  data: T[],
  fileName: string = 'Bao_Cao.xlsx',
  sheetName: string = 'DuLieu'
) {
  if (!data || data.length === 0) {
    notify.warning('Không có dữ liệu để xuất Excel.')
    return
  }

  const worksheet = XLSX.utils.json_to_sheet(data)

  // Tự động tính độ rộng cột
  const colWidths = Object.keys(data[0]).map((key) => {
    const maxLen = Math.max(
      key.length,
      ...data.map((item) => (item[key] ? String(item[key]).length : 0))
    )
    return { wch: Math.min(Math.max(maxLen + 4, 12), 50) }
  })
  worksheet['!cols'] = colWidths

  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, worksheet, sheetName)

  const finalFileName = fileName.endsWith('.xlsx') ? fileName : `${fileName}.xlsx`
  XLSX.writeFile(workbook, finalFileName)
}

/**
 * Đọc file Excel (.xlsx, .xls) và chuyển đổi thành mảng JSON
 */
export function parseExcelFile<T = any>(file: File): Promise<T[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()

    reader.onload = (e) => {
      try {
        const buffer = e.target?.result
        const workbook = XLSX.read(buffer, { type: 'binary' })
        const firstSheetName = workbook.SheetNames[0]
        const worksheet = workbook.Sheets[firstSheetName]
        const jsonData = XLSX.utils.sheet_to_json<T>(worksheet, { defval: '' })
        resolve(jsonData)
      } catch (err) {
        reject(err)
      }
    }

    reader.onerror = (err) => {
      reject(err)
    }

    reader.readAsBinaryString(file)
  })
}

/**
 * Tải file Excel mẫu để người dùng nhập liệu
 */
export function downloadExcelTemplate(
  templateHeaders: Record<string, any>[],
  fileName: string = 'Mau_Nhap_Lieu.xlsx',
  sheetName: string = 'MauNhapLieu'
) {
  const worksheet = XLSX.utils.json_to_sheet(templateHeaders)
  const colWidths = Object.keys(templateHeaders[0] || {}).map((key) => ({
    wch: Math.max(key.length + 5, 18),
  }))
  worksheet['!cols'] = colWidths

  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, worksheet, sheetName)

  const finalFileName = fileName.endsWith('.xlsx') ? fileName : `${fileName}.xlsx`
  XLSX.writeFile(workbook, finalFileName)
}

/**
 * Tải file Excel mẫu chuẩn nhập chéo cho cả Nhân Sự và Phương Tiện
 */
export function downloadCrossImportTemplate(
  fileName: string = 'Mau_Nhap_NhanSu_PhuongTien.xlsx',
  sheetName: string = 'MauNhapChieu'
) {
  const template = [
    {
      'Mã Định Danh': 'NS001',
      'Họ Và Tên': 'Nguyễn Văn An',
      'Phân Loại Nhân Sự (Employee/Contractor/Visitor/VIP)': 'Employee',
      'Số Điện Thoại': '0901234567',
      'Email': 'an.nguyen@example.com',
      'Phòng Ban': 'Phòng Kỹ Thuật',
      'Công Ty': 'Công ty CP Phú Xuân',
      'Nhà Thầu': '',
      'Biển Số Xe': '29C-123.45',
      'Loại Phương Tiện (Car/Motorcycle/Truck/Other)': 'Truck',
      'Khối Lượng (m3)': '15.5',
      'Trạng Thái (Hoạt động/Tạm dừng)': 'Hoạt động',
    },
    {
      'Mã Định Danh': 'NS002',
      'Họ Và Tên': 'Trần Thị Bình',
      'Phân Loại Nhân Sự (Employee/Contractor/Visitor/VIP)': 'Employee',
      'Số Điện Thoại': '0912345678',
      'Email': 'binh.tran@example.com',
      'Phòng Ban': 'Phòng Kế Toán',
      'Công Ty': 'Công ty CP Phú Xuân',
      'Nhà Thầu': '',
      'Biển Số Xe': '30H-888.88',
      'Loại Phương Tiện (Car/Motorcycle/Truck/Other)': 'Car',
      'Khối Lượng (m3)': '2.5',
      'Trạng Thái (Hoạt động/Tạm dừng)': 'Hoạt động',
    },
    {
      'Mã Định Danh': 'NS003',
      'Họ Và Tên': 'Lê Văn Cường',
      'Phân Loại Nhân Sự (Employee/Contractor/Visitor/VIP)': 'Contractor',
      'Số Điện Thoại': '0987654321',
      'Email': '',
      'Phòng Ban': '',
      'Công Ty': '',
      'Nhà Thầu': 'Nhà thầu Nam Cường',
      'Biển Số Xe': '',
      'Loại Phương Tiện (Car/Motorcycle/Truck/Other)': '',
      'Khối Lượng (m3)': '',
      'Trạng Thái (Hoạt động/Tạm dừng)': 'Hoạt động',
    },
    {
      'Mã Định Danh': '',
      'Họ Và Tên': '',
      'Phân Loại Nhân Sự (Employee/Contractor/Visitor/VIP)': '',
      'Số Điện Thoại': '',
      'Email': '',
      'Phòng Ban': '',
      'Công Ty': '',
      'Nhà Thầu': '',
      'Biển Số Xe': '51D-999.99',
      'Loại Phương Tiện (Car/Motorcycle/Truck/Other)': 'Truck',
      'Khối Lượng (m3)': '24.0',
      'Trạng Thái (Hoạt động/Tạm dừng)': 'Hoạt động',
    },
  ]

  downloadExcelTemplate(template, fileName, sheetName)
}

/**
 * Phân tích dữ liệu JSON từ Excel thành danh sách đối tượng nhập chéo VehiclePersonCrossImportItem
 */
export function parseCrossImportExcel(rawData: any[]): import('@/types').VehiclePersonCrossImportItem[] {
  if (!rawData || !rawData.length) return []

  return rawData.map((row) => {
    // --- PHẦN NHÂN SỰ ---
    const code = String(row['Mã Định Danh'] || row['Mã Nhân Sự'] || row['Mã Nhân Viên'] || row['personCode'] || row['code'] || '').trim()
    const fullName = String(row['Họ Và Tên'] || row['Họ Tên'] || row['fullName'] || row['name'] || '').trim()
    
    const rawType = String(row['Phân Loại Nhân Sự (Employee/Contractor/Visitor/VIP)'] || row['Phân Loại Nhân Sự'] || row['Phân Loại'] || row['personType'] || '').trim()
    let personType: import('@/types').PersonType = 'Employee'
    if (rawType.toLowerCase().includes('contractor') || rawType.toLowerCase().includes('thầu')) personType = 'Contractor'
    else if (rawType.toLowerCase().includes('visitor') || rawType.toLowerCase().includes('khách')) personType = 'Visitor'
    else if (rawType.toLowerCase().includes('vip')) personType = 'VIP'

    const phoneNumber = String(row['Số Điện Thoại'] || row['Điện Thoại'] || row['SĐT'] || row['phoneNumber'] || row['phone'] || '').trim() || undefined
    const email = String(row['Email'] || row['Email Liên Hệ'] || row['email'] || '').trim() || undefined
    const deptName = String(row['Phòng Ban'] || row['Bộ Phận'] || row['departmentName'] || row['dept'] || '').trim() || undefined
    const compName = String(row['Công Ty'] || row['Đơn Vị'] || row['companyName'] || row['company'] || '').trim() || undefined
    const contrName = String(row['Nhà Thầu'] || row['contractorName'] || row['contractor'] || '').trim() || undefined

    // --- PHẦN PHƯƠNG TIỆN ---
    const plate = String(row['Biển Số Xe'] || row['Biển Số'] || row['plateNumber'] || row['plate'] || '').trim()
    
    const rawVehType = String(row['Loại Phương Tiện (Car/Motorcycle/Truck/Other)'] || row['Loại Phương Tiện'] || row['Loại Xe (Car/Motorcycle/Truck)'] || row['Loại Xe'] || row['vehicleType'] || '').toLowerCase()
    let vehicleType: import('@/types').VehicleType = 'Car'
    if (rawVehType.includes('motor') || rawVehType.includes('máy')) vehicleType = 'Motorcycle'
    else if (rawVehType.includes('truck') || rawVehType.includes('tải')) vehicleType = 'Truck'
    else if (rawVehType.includes('other') || rawVehType.includes('khác')) vehicleType = 'Other'

    const rawWeight = row['Khối Lượng (m3)'] || row['Khối Lượng (m³)'] || row['Khối Lượng (M3)'] || row['Khối Lượng (Tấn)'] || row['Khối Lượng Xe (m3)'] || row['Khối Lượng Xe (m³)'] || row['Khối Lượng Xe (Tấn)'] || row['Khối Lượng'] || row['weight']
    const weightVal = rawWeight !== undefined && rawWeight !== null && String(rawWeight).trim() !== ''
      ? parseFloat(String(rawWeight))
      : undefined

    const rawStatus = String(row['Trạng Thái (Hoạt động/Tạm dừng)'] || row['Trạng Thái'] || row['isActive'] || '').toLowerCase()
    const isActive = !rawStatus.includes('tạm dừng') && !rawStatus.includes('dừng') && !rawStatus.includes('false')

    return {
      personCode: code || undefined,
      fullName: fullName || undefined,
      personType,
      phoneNumber,
      email,
      departmentName: deptName,
      companyName: compName,
      contractorName: contrName,
      personIsActive: isActive,
      plateNumber: plate || undefined,
      vehicleType: plate ? vehicleType : undefined,
      weight: (weightVal !== undefined && !isNaN(weightVal)) ? weightVal : undefined,
      vehicleIsActive: isActive,
    }
  }).filter((item) => item.plateNumber || item.personCode || item.fullName)
}
