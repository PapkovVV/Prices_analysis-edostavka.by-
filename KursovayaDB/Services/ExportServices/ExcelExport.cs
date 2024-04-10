using ClosedXML.Excel;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace KursovayaDB.Services.ExportServices;

public class ExcelExport
{
    public static void ExcelImportAndOpen(DataGrid dataGrid, string name, string title, bool isNewFile)
    {
        string filePathExcel = isNewFile ? $@"ExportedFiles\Excel\Exported{name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" :
            $@"ExportedFiles\Excel\Exported{name}.xlsx";
        const string excelPath = @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE";
        const string sheetName = "Sheet1";


        List<string> columnHeaders = new List<string>();//Названия столбцов
        foreach (var column in dataGrid.Columns)
        {
            if (column is DataGridTextColumn textColumn)
            {
                columnHeaders.Add(textColumn.Header.ToString());
            }
        }

        List<string> cellValues = new List<string>();//Названия столбцов

        foreach (var item in dataGrid.Items)
        {
            if (item is DataRowView row)
            {

                foreach (var cellValue in row.Row.ItemArray)
                {
                    cellValues.Add(cellValue.ToString());
                }
                cellValues.Add("end");
            }
        }



        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            for (int i = 0; i < title.Split("\n").Count(); i++)
            {
                SetHeaderRow(worksheet, i + 1, title.Split("\n")[i].Trim());
            }


            SetHeaderRow(worksheet, 6, columnHeaders.ToArray());

            int row = 7;
            int cell = 1;
            foreach (var cellValue in cellValues)
            {
                if (cellValue.Equals("end"))
                {
                    row++; cell = 1;
                    continue;
                }
                worksheet.Cell(row, cell).Value = cellValue;
                cell++;
            }
            
            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(filePathExcel);

            OpenFile(filePathExcel, excelPath);
        }

    }

    private static void SetHeaderRow(IXLWorksheet worksheet, int row, params string[] headers)//Заполнение названий столбцов EXCEL(Optimized)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(row, i + 1).Value = headers[i];
            worksheet.Cell(row, i + 1).Style.Font.SetBold(true);
            worksheet.Cell(row, i + 1).Style.Font.SetFontSize(16);
        }
    }
    public static void OpenFile(string filePath, string programmPath) //Открытие файла(Optimized)
    {
        try
        {
            Process.Start(programmPath, filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }
    }

}
