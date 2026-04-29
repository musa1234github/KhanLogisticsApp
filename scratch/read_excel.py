import pandas as pd
import sys

try:
    df = pd.read_excel(r'e:\Musa\VS SQL PROJ 2026\KTC DOTNET SQL 2026\Ultra Gst April-2026.xlsx')
    print(df.head(10).to_string())
except Exception as e:
    print(f"Error: {e}")
