import pandas as pd
import sys

file_path = r"e:\Musa\VS SQL PROJ 2026\KTC DOTNET SQL 2026\wwwroot\files\21.04.2026  MANIKGARH.xlsx"

try:
    xl = pd.ExcelFile(file_path)
    print(f"Sheets: {xl.sheet_names}")
    for sheet in xl.sheet_names:
        df = xl.parse(sheet, header=None)
        print(f"Sheet '{sheet}' shape: {df.shape}")
        # Print first 20 rows to see headers
        print(df.head(20))
except Exception as e:
    print(f"Error: {e}")
