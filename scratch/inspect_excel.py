import pandas as pd
import sys

files = ['JSW_BillUpload.xlsx', 'MANIGARH_BillUpload_WithQty.xlsx', 'ULTRA_BillUpload_WithQty.xlsx']

for f in files:
    print(f"--- {f} ---")
    try:
        df = pd.read_excel(f, nrows=2)
        print(df.columns.tolist())
        print(df.iloc[0].tolist())
    except Exception as e:
        print(f"Error reading {f}: {e}")
