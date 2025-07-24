using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Menu_Management.Class
{
    internal class DatabaseHelper
    {
        internal static Image convertToImage(byte[] data)
        {
            byte[] imgData = data; //mảng chứa data của ảnh lấy từ CSDL (lúc này chính là byte)
            Image img; //tạo biến Image để lưu ảnh sau khi convert
            if (imgData != null) //khi ảnh được đọc lên khác null
            {
                using (MemoryStream ms = new MemoryStream(imgData)) //sử dụng đối tượng MemoryStream để lưu data ảnh (byte) được đọc vào luồng trực tiếp (bộ nhớ thay vì ổ cứng)
                {
                    img = Image.FromStream(ms); //chuyển đổi 1 luồng dữ liệu (byte) thành dạng ảnh thông qua FromStream và lưu vào biến img trước đó đã tạo
                    return img;
                }
            }
            else
            {
                return null;
            }
        }

        internal static string GetConnectionString()
        {
            // Replace with your actual database connection string
            return "Data Source=localhost;Initial Catalog=Restaurant_Menu;Integrated Security=True;Trust Server Certificate=True";
        }



        internal static void ShowCategory(FlowLayoutPanel fl,FlowLayoutPanel Orderfl,Label TotalLabel, FlowLayoutPanel DishShowPanel, HomeForm hf)
        {
            fl.Controls.Clear(); //Xóa tất cả các điều khiển trong FlowLayoutPanel trước khi thêm mới
            using (SqlConnection sqlcon = new SqlConnection(GetConnectionString()))
            {
                sqlcon.Open();
                SqlCommand sqlcmd = new SqlCommand("SELECT * FROM Categories", sqlcon);
                SqlDataReader reader = sqlcmd.ExecuteReader();
                if (!reader.HasRows) //Kiểm tra xem có danh mục nào không
                {
                    MessageBox.Show("No categories found in the database.\nPlease the README.md file for this", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit(); //Nếu không có danh mục nào thì thoát ứng dụng
                }
                while (reader.Read())
                {
                    string categoryName = reader["CategoryName"].ToString();
                    byte[] cateimgdata = reader["CategoryIMG"] as byte[];
                    string ID = reader["CategoryID"].ToString();
                    Image categoryImage = convertToImage(cateimgdata);
                    UC_CategoryItem categoryItem = new UC_CategoryItem(categoryName, categoryImage, ID);


                    categoryItem.OnCategorySelect += (sender, e) => //Gán hành động cho sự kiện đã khai báo trước
                    {
                        MainHelper.currentCategoryID = ID; //Cập nhật ID danh mục hiện tại
                        hf.CategoryLBL.Text = MainHelper.GetCurrentCategory(ID); //Cập nhật tiêu đề danh mục
                        ShowDishes(DishShowPanel, Orderfl, TotalLabel, ID); //tại vì ở UC không hề reference được tới FlowPanel của form này nên phải gán sự kiện ở đây
                    };
                    fl.Controls.Add(categoryItem);
                }
            }
        }

        internal static void ShowDishesBySearch(FlowLayoutPanel fl, string text)
        {
            fl.Controls.Clear();
            using (SqlConnection sqlcon = new SqlConnection(GetConnectionString()))
            {
                sqlcon.Open();
                string query = "SELECT * FROM Dishes WHERE DishName LIKE @text AND IsDeleted = 0";
                SqlCommand sqlcmd = new SqlCommand(query, sqlcon);
                sqlcmd.Parameters.AddWithValue("@text", '%' + text + '%'); //Thêm tham số để tránh SQL Injection

                SqlDataReader reader = sqlcmd.ExecuteReader();  

                if (!reader.HasRows)
                {
                    fl.Controls.Add(new Label() { Text = "No dishes found matching your search.", AutoSize = true });
                    return;
                }
                while (reader.Read())
                {
                    string dishName = reader["DishName"].ToString();
                    float price = float.Parse(reader["Price"].ToString());
                    byte[] dishImagedata = reader["DishIMG"] as byte[];
                    Image DishImage = convertToImage(dishImagedata);
                    string ID = reader["DishID"].ToString();
                    string dishtypeID = reader["CategoryID"].ToString();
                    UC_MenuItem Dish = new UC_MenuItem(dishName, price, DishImage, ID, dishtypeID);
                    fl.Controls.Add(Dish);
                }
            }    
        }
        internal static void ShowDishes(FlowLayoutPanel fl,FlowLayoutPanel Orderfl,Label totalpriceLabel, string categoryselection = null)
        {
            fl.Controls.Clear();
            using (SqlConnection sqlcon = new SqlConnection(GetConnectionString()))
            {
                sqlcon.Open();
                string dishfilteredQuery = "SELECT * FROM Dishes WHERE CategoryID = @categoryid AND IsDeleted = 0";
                string fulldishQuery = "SELECT * FROM Dishes WHERE IsDeleted = 0";
                SqlCommand sqlcmd = new SqlCommand();
                sqlcmd.Connection = sqlcon;
                if (string.IsNullOrEmpty(categoryselection))
                {
                    sqlcmd.CommandText = fulldishQuery;
                }
                else
                {
                    sqlcmd.CommandText = dishfilteredQuery;
                    sqlcmd.Parameters.AddWithValue("@categoryid", categoryselection);
                }
                SqlDataReader reader = sqlcmd.ExecuteReader();
                while (reader.Read())
                {
                    string dishName = reader["DishName"].ToString();
                    float price = float.Parse(reader["Price"].ToString());
                    byte[] dishImagedata = reader["DishIMG"]as byte[];
                    Image DishImage = convertToImage(dishImagedata);
                    string ID = reader["DishID"].ToString();
                    string dishtypeID = reader["CategoryID"].ToString();
                    UC_MenuItem Dish = new UC_MenuItem(dishName, price, DishImage, ID, dishtypeID);
                    Dish.DishSelected += (sender, e) => //Gán hành động cho sự kiện đã khai báo trước
                    {
                        if(OrderHelper.CurrentOrderID == 0) //Kiểm tra xem đã có đơn hàng nào được tạo chưa
                        {
                            OrderHelper.CurrentOrderID = new Random().Next(1000, 9999); //Tạo ID đơn hàng ngẫu nhiên nếu chưa có
                        }
                        UC_OrderItem orderItem = new UC_OrderItem(dishName, price, DishImage, ID, dishtypeID);
                        orderItem.removeOrderItem += (s, ev) => //Gán hành động cho sự kiện đã khai báo trước
                        {
                            Orderfl.Controls.Remove(orderItem); //Xóa món ăn đã chọn khỏi FlowLayoutPanel Orderfl
                            totalpriceLabel.ResetText();
                            OrderHelper.CalculateTotalPrice(Orderfl);
                            totalpriceLabel.Text = OrderHelper.totalPrice.ToString();
                        };
                        orderItem.quantityChanged += (sender, ev) => //Gán hành động cho sự kiện đã khai báo trước
                        {
                            OrderHelper.CalculateTotalPrice(Orderfl);
                            totalpriceLabel.ResetText();
                            totalpriceLabel.Text = OrderHelper.totalPrice.ToString();
                        };
                        Orderfl.Controls.Add(orderItem); //Thêm món ăn đã chọn vào FlowLayoutPanel Orderfl
                        totalpriceLabel.ResetText();
                        OrderHelper.CalculateTotalPrice(Orderfl);
                        totalpriceLabel.Text = OrderHelper.totalPrice.ToString();
                    };
                    fl.Controls.Add(Dish);
                }
                reader.Close();
            }
        }

        internal static void ShowEmployee(DataGridView datagridview)
        {
            datagridview.Columns.Clear(); //Xóa các cột hiện tại trong DataGridView
            datagridview.ColumnHeadersHeight = 30;
            datagridview.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 107, 0);
            datagridview.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            using (SqlConnection sqlcon = new SqlConnection(GetConnectionString()))
            {
                sqlcon.Open();

                string q = "SELECT UserName, FullName, Gender, RoleName, Status FROM Accounts\r\nJOIN Roles ON Roles.RoleID = Accounts.RoleID";
                SqlDataAdapter adapter = new SqlDataAdapter(q, sqlcon);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                datagridview.DataSource = dt;
            }    
        }
        internal static void LoadRoles(ComboBox roleCombobox)
        {
            using (SqlConnection sqlcon = new SqlConnection(GetConnectionString()))
            {
                sqlcon.Open();
                string rolequery = "SELECT RoleName FROM Roles";
                SqlCommand sqlcmd = new SqlCommand(rolequery, sqlcon);
                SqlDataReader reader = sqlcmd.ExecuteReader();
                while(reader.Read())
                {
                    string roleName = reader["RoleName"].ToString();
                    roleCombobox.Items.Add(roleName);
                }    
            }    
        }
    }
}
