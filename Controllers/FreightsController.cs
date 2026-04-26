using KhanLogistics.Models;
using KhanLogistics.Models.ViewModels;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
namespace KhanLogistics.Controllers
{
    public class FreightsController : Controller
    {
        TransportMgmtContext _transportMgmtContext;
        IConfiguration _configuration;
        IWebHostEnvironment _hostingEnvironment;
        IExcelDataReader _excelDataReader;
        //IHostingEnvironment _hostingEnvironment;
        public FreightsController(TransportMgmtContext context, IWebHostEnvironment webHostEnvironment)
        {
           this._transportMgmtContext = context;
           //this._excelDataReader = excelDataReader;
           //this._configuration  = configuration;
           this._hostingEnvironment = webHostEnvironment;
           //this._hostingEnvironment = hostingEnvironment;
           
        }

        public IActionResult ShowFreight()
        {
            FreightVModel model = new FreightVModel();
            model.ddlVendors = _transportMgmtContext.TblFactories.ToList().Select(a => new SelectListItem()
            {
                Text = a.FactoryName,
                Value = Convert.ToString(a.FID)
            }).ToList();
            model.freightVm = _transportMgmtContext.TblFreights.ToList().Select(a => new FreightVmNew()
            {
                DestId = a.DestId,
                Vid = Convert.ToInt32(a.Vid),
                CompanyName = a.CompanyName,
                Destination = a.Destination,
                FreightRate = Convert.ToDouble(a.FreightRate),
                Wheels = a.Wheels,
                Freight = a.Quantity

            }).AsEnumerable();

            return View("ShowFreight", model);
        }
        [HttpPost]
        public async Task<IActionResult> UploadFreight(IFormFile file) 
        {
            List<TblFreight> lstFreights = new List<TblFreight>();
            try
            {
                string filename = $"{_hostingEnvironment.WebRootPath}\\files\\{file.FileName}";
                string dirpath = Path.Combine(_hostingEnvironment.WebRootPath, "files");
                string datafilename = Path.GetFileName(file.FileName);
                string savetopath = Path.Combine(dirpath, datafilename);
                string extention = Path.GetExtension(datafilename);
                using (FileStream stream = new FileStream(savetopath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Flush();
                }
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
               
                using (var stream = new FileStream(savetopath, FileMode.Open))
                {
                    //if (extention == ".xls")
                    //{
                    //    _excelDataReader = ExcelReaderFactory.CreateBinaryReader(stream);

                    //}
                    //else
                    //{
                    //    _excelDataReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                    //}
                    _excelDataReader = ExcelReaderFactory.CreateReader(stream);
                    DataSet dts = new DataSet();
                    dts = _excelDataReader.AsDataSet();
                    _excelDataReader.Close();

                    if (dts != null && dts.Tables.Count > 0)
                    {
                        int vid = -1;
                        DataTable dataTable = dts.Tables[0];
                        string tbName = dataTable.TableName;
                        if (tbName == "AMBUJA")
                        {
                            vid = 1;
                        }
                        if (tbName == "ACC CEMENT")
                        {
                            vid = 5;
                        }
                        if (tbName == "ULTRATECH")
                        {
                            vid = 7;
                        }
                       

                        for (int i = 1; i < dataTable.Rows.Count; i++)
                        {
                            var cell1 = Convert.ToString(dataTable.Rows[i][0]);
                            var cell2 = Convert.ToString(dataTable.Rows[i][1]);
                            var cell3 = Convert.ToString(dataTable.Rows[i][2]);
                            var cell4 = Convert.ToDouble(dataTable.Rows[i][3]);
                            var cell5 = Convert.ToString(dataTable.Rows[i][4]);


                            if (string.IsNullOrWhiteSpace(cell1) || string.IsNullOrWhiteSpace(cell2)) 
                            {

                                continue;
                            }
                            bool isexist = _transportMgmtContext.TblFreights.Any(d => d.Destination == cell2 && d.Quantity == cell3);
                            if (isexist == false)
                            {
                                lstFreights.Add(new TblFreight()
                                {
                                    CompanyName = Convert.ToString(cell1),
                                    Destination = Convert.ToString(cell2),
                                    Quantity = Convert.ToString(cell3),
                                    FreightRate = Convert.ToDouble(cell4),
                                    Wheels = Convert.ToString(cell5),
                                    Vid = vid

                                });
                            }
                        }
                    }
                };
                _transportMgmtContext.AddRange(lstFreights);
                _transportMgmtContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string err = ex.ToString();
            }
          

            int cnt = _transportMgmtContext.TblFreights.Count();
            //return View("ShowFreight", lstFreights.AsEnumerable());
            FreightVModel model = new FreightVModel();
            model.ddlVendors = _transportMgmtContext.TblFactories.ToList().Select(f => new SelectListItem()
            {
                Text = f.FactoryName,
                Value = Convert.ToString(f.FID)
            }).ToList();
            model.freightVm = _transportMgmtContext.TblFreights.ToList().Select(a => new FreightVmNew()
            //model.freightVm = lstFreights.ToList().Select(a => new FreightVmNew()
            {
                DestId = a.DestId,
                Vid = Convert.ToInt32(a.Vid),
                CompanyName = a.CompanyName,
                Destination = a.Destination,
                FreightRate = Convert.ToDouble(a.FreightRate),
                Wheels = a.Wheels,
                Freight = a.Quantity

            }).AsEnumerable();

            return View("ShowFreight", model);
        }

        
    }





    //public IActionResult FreightUpload()
    //{
    //    //string s = null;
    //    var d = new DirectoryInfo(@"C:\Test");
    //    var files = d.GetFiles("*.xlsx");
    //    List<TblFreight> tblFreight = new List<TblFreight>();
    //    foreach (var file in files)
    //    {
    //        var fileName = file.FullName;

    //        using var package = new ExcelPackage(file);
    //        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
    //        var currentSheet = package.Workbook.Worksheets;
    //        var workSheet = currentSheet.First();
    //        var noOfCol = workSheet.Dimension.End.Column;
    //        var noOfRow = workSheet.Dimension.End.Row;
    //        for (int rowIterator = 5    ; rowIterator <= noOfRow; rowIterator++)
    //        {
    //            var freight = new TblFreight()
    //            {
    //                DestId = rowIterator,
    //                CompanyName = Convert.ToString(workSheet.Cells[rowIterator, 2].Value).ToString(),
    //                Destination   = Convert.ToString(workSheet.Cells[rowIterator, 2].Value).ToString(),
    //                Quantity = Convert.ToString(workSheet.Cells[rowIterator, 2].Value).ToString(),
    //                FreightRate = Convert.ToDouble(workSheet.Cells[rowIterator, 2].Value),
    //                Wheels = Convert.ToString(workSheet.Cells[rowIterator, 2].Value).ToString(),


    //            };


    //            tblFreight.Add(freight  );
    //        }
    //        _transportMgmtContext.SaveChanges();

    //    }

    //    return View();
    //}


}

