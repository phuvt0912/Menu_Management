using FastReport;
using Menu_Management.Class;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FastReport.Export.PdfSimple;

namespace Menu_Management
{
    public partial class AdminCheckoutFrm : Form
    {
        public AdminCheckoutFrm()
        {
            InitializeComponent();
        }

        private void ReportItemWise_Click(object sender, EventArgs e)
        {
            DateTime fromDate = StartDate.Value.Date;
            DateTime toDate = EndDate.Value.Date.AddDays(1).AddTicks(-1);
            if(fromDate > toDate)
            {
                MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if(fromDate > DateTime.Now || toDate > DateTime.Now)
            {
                MessageBox.Show("Ngày bắt đầu và ngày kết thúc không được lớn hơn ngày hiện tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.GetConnectionString()))
            {
                conn.Open();
                string query = @"
            SELECT 
                D.DishName,
                BD.UnitPrice,
                SUM(BD.Quantity) AS SoLuong,
                SUM(BD.Quantity * BD.UnitPrice) AS ThanhTien
            FROM BillDetails BD
            JOIN Dishes D ON BD.DishID = D.DishID
            JOIN Bills B ON BD.BillID = B.BillID
            WHERE B.OrderTime BETWEEN @FromDate AND @ToDate
                  AND B.Status = N'Done'
            GROUP BY D.DishName, BD.UnitPrice

            UNION ALL

            SELECT 
                N'TỔNG CỘNG' AS DishName,
                NULL AS UnitPrice,
                SUM(BD.Quantity),
                SUM(BD.Quantity * BD.UnitPrice)
            FROM BillDetails BD
            JOIN Dishes D ON BD.DishID = D.DishID
            JOIN Bills B ON BD.BillID = B.BillID
            WHERE B.OrderTime BETWEEN @FromDate AND @ToDate
                  AND B.Status = N'Done';
        ";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FromDate", fromDate);
                cmd.Parameters.AddWithValue("@ToDate", toDate);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            Report report = new Report();
            try
            {
                report.Load("C:/Users/Phu/Desktop/.net project/Menu Management/Reports/ItemWiseReport.frx"); //đây là đường dẫn cố định
            }
            catch (Exception ex)
            {
                MessageBox.Show("Vui lòng đọc file README.md\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //thêm tham số cho báo cáo
            report.SetParameterValue("FromDate", fromDate);
            report.SetParameterValue("ToDate", toDate);
            //thêm dữ liệu vào báo cáo
            report.RegisterData(dt,"Revenue");
            report.GetDataSource("Revenue").Enabled = true;

            report.Prepare();

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); //lấy đường dẫn mặc định đến Desktop

            string exportFolder = Path.Combine(desktopPath, "MenuReports");  //tạo thư mục "MenuReports" trên Desktop nếu chưa tồn tại
            if(!Directory.Exists(exportFolder)) //kiểm tra nếu thư mục không tồn tại
            {
                Directory.CreateDirectory(exportFolder); //kiểm tra nếu thư mục không tồn tại thì tạo mới
            }    
            string exportPath = Path.Combine(exportFolder, $"DoanhThu_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}.pdf"); //đặt tên file xuất báo cáo với định dạng ngày tháng
            PDFSimpleExport pdfExport = new PDFSimpleExport();
            report.Export(pdfExport, exportPath);
            report.Dispose();

            MessageBox.Show("Đã xuất báo cáo thành công:\n" + exportPath, "FastReport", MessageBoxButtons.OK);
        }
    }
}
