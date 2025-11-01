using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class QA_Form1_BCCLC_VN_android_2toleranceController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<QA_Form1_BCCLC_VN_android_2toleranceController> _logger;

        public QA_Form1_BCCLC_VN_android_2toleranceController(IConfiguration config, ILogger<QA_Form1_BCCLC_VN_android_2toleranceController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost]
        public IActionResult QA_Form1_BCCLC_VN_android_2tolerance([FromBody] BCCLCModel request)
        {
            if (request == null)
                return BadRequest(new { error = "Invalid request data" });

            try
            {
                string result = "";

                switch (request.Act?.ToLower())
                {
                    case "insert":
                        result = InsertBCCLC(request);
                        break;
                    case "update":
                        if (string.IsNullOrEmpty(request.ID))
                            return BadRequest(new { error = "ID is required for update" });
                        result = UpdateBCCLC(request);
                        break;
                    case "delete":
                        if (string.IsNullOrEmpty(request.ID))
                            return BadRequest(new { error = "ID is required for delete" });
                        result = DeleteBCCLC(request);
                        break;
                    default:
                        return BadRequest(new { error = "Invalid action. Use 'Insert', 'Update', or 'Delete'" });
                }

                if (result == "success")
                    return Ok(new { message = "success" });
                else
                    return StatusCode(500, new { error = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, $"Database error in SaveBCCLC - Action: {request.Act}");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SaveBCCLC - Action: {request.Act}");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        private string InsertBCCLC(BCCLCModel request)
        {
            using (SqlConnection conn = new(_connectionString))
            {
                string query = @"
                    INSERT INTO Form1_BCCLC
                    (
                        [Report_ID], [Unit], [Cut_Leader], [Cut_Lot], [Lay_Height],
                        [Table_Long], [Table_Width], [MO], [Color], [CutTableName],
                        [CutTableRatio], [Batch], [Cut_QTY], [Shading], [Wave],
                        [Narrow_Width], [Spreading], [DS_L_Min], [DS_L_Max], [DS_W_Min],
                        [DS_W_Max], [Size_Parameter], [Notch], [Unclean], [Straigh],
                        [Shape], [Edge], [Stripe], [Remark], [Re_Audit],
                        [UserUpdate], [LastUpdate], [Re_Audit_Time], [Photo_URL], [Audit_Time],
                        [DS_L_Min_2], [DS_L_Max_2], [DS_W_Min_2], [DS_W_Max_2]
                    )
                    VALUES
                    (
                        @Report_ID, @Unit, @Cut_Leader, @Cut_Lot, @Lay_Height,
                        @Table_Long, @Table_Width, @MO, @Color, @CutTableName,
                        @CutTableRatio, @Batch, @Cut_QTY, @Shading, @Wave,
                        @Narrow_Width, @Spreading, @DS_L_Min, @DS_L_Max, @DS_W_Min,
                        @DS_W_Max, @Size_Parameter, @Notch, @Unclean, @Straigh,
                        @Shape, @Edge, @Stripe, @Remark, @Re_Audit,
                        @UserUpdate, @LastUpdate, NULL, @Photo_URL, @Audit_Time,
                        @DS_L_Min_2, @DS_L_Max_2, @DS_W_Min_2, @DS_W_Max_2
                    )";

                using (SqlCommand cmd = new(query, conn))
                {
                    AddBCCLCParameters(cmd, request);
                 
                    cmd.Parameters.AddWithValue("@LastUpdate", DateTime.Now);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    
                    return rowsAffected > 0 ? "success" : "error";
                }
            }
        }

        private string UpdateBCCLC(BCCLCModel request)
        {
            using (SqlConnection conn = new(_connectionString))
            {
                string query = @"
                    UPDATE Form1_BCCLC SET
                        [Unit] = @Unit,
                        [Cut_Leader] = @Cut_Leader,
                        [Cut_Lot] = @Cut_Lot,
                        [Lay_Height] = @Lay_Height,
                        [Table_Long] = @Table_Long,
                        [Table_Width] = @Table_Width,
                        [MO] = @MO,
                        [Color] = @Color,
                        [CutTableName] = @CutTableName,
                        [CutTableRatio] = @CutTableRatio,
                        [Batch] = @Batch,
                        [Cut_QTY] = @Cut_QTY,
                        [Shading] = @Shading,
                        [Wave] = @Wave,
                        [Narrow_Width] = @Narrow_Width,
                        [Spreading] = @Spreading,
                        [DS_L_Min] = @DS_L_Min,
                        [DS_L_Max] = @DS_L_Max,
                        [DS_W_Min] = @DS_W_Min,
                        [DS_W_Max] = @DS_W_Max,
                        [Size_Parameter] = @Size_Parameter,
                        [Notch] = @Notch,
                        [Unclean] = @Unclean,
                        [Straigh] = @Straigh,
                        [Shape] = @Shape,
                        [Edge] = @Edge,
                        [Stripe] = @Stripe,
                        [Remark] = @Remark,
                        [Re_Audit] = @Re_Audit,
                        [Re_Audit_Time] = NULL,
                        [Photo_URL] = @Photo_URL,
                        [User_Edit] = @User_Edit,
                        [Date_Edit] = @Date_Edit,
                        [DS_L_Min_2] = @DS_L_Min_2,
                        [DS_L_Max_2] = @DS_L_Max_2,
                        [DS_W_Min_2] = @DS_W_Min_2,
                        [DS_W_Max_2] = @DS_W_Max_2
                    WHERE ID = @ID";

                using (SqlCommand cmd = new(query, conn))
                {
                    AddBCCLCParameters(cmd, request);
                    cmd.Parameters.AddWithValue("@ID", request.ID);
                    cmd.Parameters.AddWithValue("@Date_Edit", DateTime.Now);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    
                    return rowsAffected > 0 ? "success" : "error";
                }
            }
        }

        private string DeleteBCCLC(BCCLCModel request)
        {
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Insert vào bảng backup
                        string insertBackupQuery = @"
                            INSERT INTO Form1_BCCLC_Delete
                            (
                                [ID], [Report_ID], [Unit], [Cut_Leader], [CutTableName],
                                [Lay_Height], [Table_Long], [Table_Width], [CutTableRatio], [Cut_Lot],
                                [MO], [Color], [Batch], [Cut_QTY], [Shading],
                                [Wave], [Narrow_Width], [Spreading], [DS_L_Min], [DS_L_Max],
                                [DS_W_Min], [DS_W_Max], [Size_Parameter], [Notch], [Unclean],
                                [Straigh], [Shape], [Edge], [Stripe], [Remark],
                                [Re_Audit], [Re_Audit_Time], [Audit_Time], [UserUpdate], [LastUpdate],
                                [Photo_URL], [Rap], [User_Edit], [Date_Edit],
                                [DS_L_Min_2], [DS_L_Max_2], [DS_W_Min_2], [DS_W_Max_2],
                                [UserDelete], [TimeDelete]
                            )
                            SELECT 
                                [ID], [Report_ID], [Unit], [Cut_Leader], [CutTableName],
                                [Lay_Height], [Table_Long], [Table_Width], [CutTableRatio], [Cut_Lot],
                                [MO], [Color], [Batch], [Cut_QTY], [Shading],
                                [Wave], [Narrow_Width], [Spreading], [DS_L_Min], [DS_L_Max],
                                [DS_W_Min], [DS_W_Max], [Size_Parameter], [Notch], [Unclean],
                                [Straigh], [Shape], [Edge], [Stripe], [Remark],
                                [Re_Audit], [Re_Audit_Time], [Audit_Time], [UserUpdate], [LastUpdate],
                                [Photo_URL], [Rap], [User_Edit], [Date_Edit],
                                [DS_L_Min_2], [DS_L_Max_2], [DS_W_Min_2], [DS_W_Max_2],
                                @UserDelete, @TimeDelete
                            FROM Form1_BCCLC 
                            WHERE ID = @ID";

                        using (SqlCommand cmdBackup = new(insertBackupQuery, conn, transaction))
                        {
                            cmdBackup.Parameters.AddWithValue("@ID", request.ID);
                            cmdBackup.Parameters.AddWithValue("@UserDelete", request.User_Edit ?? "");
                            cmdBackup.Parameters.AddWithValue("@TimeDelete", DateTime.Now);
                            cmdBackup.ExecuteNonQuery();
                        }

                        // Delete bản ghi gốc
                        string deleteQuery = "DELETE FROM Form1_BCCLC WHERE ID = @ID";
                        using (SqlCommand cmdDelete = new(deleteQuery, conn, transaction))
                        {
                            cmdDelete.Parameters.AddWithValue("@ID", request.ID);
                            int rowsAffected = cmdDelete.ExecuteNonQuery();
                            
                            if (rowsAffected > 0)
                            {
                                transaction.Commit();
                                return "success";
                            }
                            else
                            {
                                transaction.Rollback();
                                return "error: Record not found";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError(ex, "Error in DeleteBCCLC transaction");
                        return "error: " + ex.Message;
                    }
                }
            }
        }

        private void AddBCCLCParameters(SqlCommand cmd, BCCLCModel request)
        {
            cmd.Parameters.AddWithValue("@Report_ID", request.Report_ID ?? "");
            cmd.Parameters.AddWithValue("@Unit", request.Unit ?? "");
            cmd.Parameters.AddWithValue("@Cut_Leader", request.Cut_Leader ?? "");
            cmd.Parameters.AddWithValue("@Cut_Lot", request.Cut_Lot ?? "");
            cmd.Parameters.AddWithValue("@Lay_Height", request.Lay_Height ?? "");
            cmd.Parameters.AddWithValue("@Table_Long", request.Table_Long ?? "");
            cmd.Parameters.AddWithValue("@Table_Width", request.Table_Width ?? "");
            cmd.Parameters.AddWithValue("@MO", request.MO ?? "");
            cmd.Parameters.AddWithValue("@Color", request.Color ?? "");
            cmd.Parameters.AddWithValue("@CutTableName", request.CutTableName ?? "");
            cmd.Parameters.AddWithValue("@CutTableRatio", request.CutTableRatio ?? "");
            cmd.Parameters.AddWithValue("@Batch", request.Batch ?? "");
            
            cmd.Parameters.AddWithValue("@QTY", request.QTY);
            cmd.Parameters.AddWithValue("@Shading", request.Shading);
            cmd.Parameters.AddWithValue("@Wave", request.Wave);
            cmd.Parameters.AddWithValue("@Narrow_Width", request.Narrow_Width);
            cmd.Parameters.AddWithValue("@Spreading", request.Spreading);
            
            cmd.Parameters.AddWithValue("@DS_L_Min", request.DS_L_Min ?? "");
            cmd.Parameters.AddWithValue("@DS_L_Max", request.DS_L_Max ?? "");
            cmd.Parameters.AddWithValue("@DS_W_Min", request.DS_W_Min ?? "");
            cmd.Parameters.AddWithValue("@DS_W_Max", request.DS_W_Max ?? "");
            
            cmd.Parameters.AddWithValue("@DS_L_Min_2", request.DS_L_Min_2 ?? "");
            cmd.Parameters.AddWithValue("@DS_L_Max_2", request.DS_L_Max_2 ?? "");
            cmd.Parameters.AddWithValue("@DS_W_Min_2", request.DS_W_Min_2 ?? "");
            cmd.Parameters.AddWithValue("@DS_W_Max_2", request.DS_W_Max_2 ?? "");
            
            cmd.Parameters.AddWithValue("@Size_Parameter", request.Size_Parameter ?? "");
            cmd.Parameters.AddWithValue("@Notch", request.Notch ?? "");
            cmd.Parameters.AddWithValue("@Unclean", request.Unclean ?? "");
            cmd.Parameters.AddWithValue("@Straigh", request.Straigh ?? "");
            cmd.Parameters.AddWithValue("@Shape", request.Shape ?? "");
            cmd.Parameters.AddWithValue("@Edge", request.Edge ?? "");
            cmd.Parameters.AddWithValue("@Stripe", request.Stripe ?? "");
            cmd.Parameters.AddWithValue("@Remark", request.Remark ?? "");
            cmd.Parameters.AddWithValue("@Re_Audit", request.Re_Audit);
            cmd.Parameters.AddWithValue("@Audit_Time", request.Audit_Time);
            cmd.Parameters.AddWithValue("@UserUpdate", request.User ?? "");
            cmd.Parameters.AddWithValue("@User_Edit", request.User_Edit ?? "");
            cmd.Parameters.AddWithValue("@Photo_URL", request.Photo_URL ?? "");
        }
    }
}