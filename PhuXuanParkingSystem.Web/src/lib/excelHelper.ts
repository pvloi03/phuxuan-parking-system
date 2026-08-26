import * as XLSX from 'xlsx'

/**
 * Xuất dữ liệu ra file Excel (.xlsx) với header đẹp và tự động căn chỉnh độ rộng cột
 */
export function exportToExcel<T extends Record<string, any>>(
  data: T[],
  fileName: string = 'Bao_Cao.xlsx',
  sheetName: string = 'DuLieu'
) {
  if (!data || data.length === 0) {
    alert('Không có dữ liệu để xuất Excel.')
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
  fileName: string = 'Mau_Nhap_Lieu.xlsx'
) {
  const worksheet = XLSX.utils.json_to_sheet(templateHeaders)
  const colWidths = Object.keys(templateHeaders[0] || {}).map((key) => ({
    wch: Math.max(key.length + 5, 18),
  }))
  worksheet['!cols'] = colWidths

  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, worksheet, 'MauNhapLieu')

  const finalFileName = fileName.endsWith('.xlsx') ? fileName : `${fileName}.xlsx`
  XLSX.writeFile(workbook, finalFileName)
}
