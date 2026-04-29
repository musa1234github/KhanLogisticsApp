import openpyxl
import sys

try:
    wb = openpyxl.load_workbook(r'e:\Musa\VS SQL PROJ 2026\KTC DOTNET SQL 2026\Ultra Gst April-2026.xlsx', data_only=True)
    sheet = wb.active
    for row in sheet.iter_rows(max_row=5):
        print([cell.value for cell in row])
except Exception as e:
    print(f"Error: {e}")
