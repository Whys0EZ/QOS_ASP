using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BCDTFormController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<BCDTFormController> _logger;

        public BCDTFormController(IConfiguration config, ILogger<BCDTFormController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost]
        public IActionResult SaveBCDT([FromBody] BCDTModel request)
        {
            // _logger.LogInformation("update report: " + request.ID);
            if (request == null)
            
                return BadRequest(new { error = "Invalid request data" });

            try
            {
                string result = "";

                switch (request.Act?.ToLower())
                {
                    case "insert":
                        result = InsertBCDT(request);
                        break;
                    case "update":
                        // _logger.LogInformation("update report: " + request.ID);
                        if (string.IsNullOrEmpty(request.ID))
                            return BadRequest(new { error = "ID is required for update" });
                        result = UpdateBCDT(request);
                        break;
                    case "delete":
                        if (string.IsNullOrEmpty(request.ID))
                            return BadRequest(new { error = "ID is required for delete" });
                        result = DeleteBCDT(request);
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
                _logger.LogError(ex, $"Database error in SaveBCDT - Action: {request.Act}");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SaveBCDT - Action: {request.Act}");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        private string InsertBCDT(BCDTModel request)
        {
            using (SqlConnection conn = new(_connectionString))
            {
                string query = @"
                    INSERT INTO Form3_BCDT
                    (
                        [Report_ID], [Unit], [Cut_Leader], [MO], [Color], [Roll], [Lot],
                        [QTY], [Layer_QTY], [CutTableRatio], [Cut_QTY], [Cut_Table_Height], [Cut_Table_Long],
                        [vai_ke], [noi_vai], [cang_vai], [sai_mat], [hep_kho], [ban_vai], [vi_tri],
                        [vai_nghieng], [song_vai], [thang_ke], [khac_mau], [quen_bam], [bam_sau],
                        [xoc_xech], [khong_cat], [khong_gon], [doi_ke], [doi_xung], [so_lop], [so_btp],
                        [Size_Parameter_Cat], [Size_Parameter_CPI], [Size_Parameter_TP],
                        [Remark], [Re_Audit], [Re_Audit_Time], [Audit_Time], [UserUpdate], [LastUpdate], [Photo_URL]
                    )
                    VALUES
                    (
                        @Report_ID, @Unit, @Cut_Leader, @MO, @Color, @Roll, @Lot,
                        @QTY, @Layer_QTY, @CutTableRatio, @Cut_QTY, @Cut_Table_Height, @Cut_Table_Long,
                        @vai_ke, @noi_vai, @cang_vai, @sai_mat, @hep_kho, @ban_vai, @vi_tri,
                        @vai_nghieng, @song_vai, @thang_ke, @khac_mau, @quen_bam, @bam_sau,
                        @xoc_xech, @khong_cat, @khong_gon, @doi_ke, @doi_xung, @so_lop, @so_btp,
                        @Size_Parameter_Cat, @Size_Parameter_CPI, @Size_Parameter_TP,
                        @Remark, @Re_Audit, @Re_Audit_Time, @Audit_Time, @UserUpdate, @LastUpdate, @Photo_URL
                    )";

                using (SqlCommand cmd = new(query, conn))
                {
                    AddBCDTParameters(cmd, request);
                    cmd.Parameters.AddWithValue("@Re_Audit_Time", DateTime.Now);
                    cmd.Parameters.AddWithValue("@LastUpdate", DateTime.Now);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    
                    return rowsAffected > 0 ? "success" : "error";
                }
            }
        }

        private string UpdateBCDT(BCDTModel request)
        {
            using (SqlConnection conn = new(_connectionString))
            {
                string query = @"
                    UPDATE Form3_BCDT SET
                        [Unit] = @Unit,
                        [Cut_Leader] = @Cut_Leader,
                        [MO] = @MO,
                        [Color] = @Color,
                        [Roll] = @Roll,
                        [Lot] = @Lot,
                        [QTY] = @QTY,
                        [Layer_QTY] = @Layer_QTY,
                        [CutTableRatio] = @CutTableRatio,
                        [Cut_QTY] = @Cut_QTY,
                        [Cut_Table_Height] = @Cut_Table_Height,
                        [Cut_Table_Long] = @Cut_Table_Long,
                        [vai_ke] = @vai_ke,
                        [noi_vai] = @noi_vai,
                        [cang_vai] = @cang_vai,
                        [sai_mat] = @sai_mat,
                        [hep_kho] = @hep_kho,
                        [ban_vai] = @ban_vai,
                        [vi_tri] = @vi_tri,
                        [vai_nghieng] = @vai_nghieng,
                        [song_vai] = @song_vai,
                        [thang_ke] = @thang_ke,
                        [khac_mau] = @khac_mau,
                        [quen_bam] = @quen_bam,
                        [bam_sau] = @bam_sau,
                        [xoc_xech] = @xoc_xech,
                        [khong_cat] = @khong_cat,
                        [khong_gon] = @khong_gon,
                        [doi_ke] = @doi_ke,
                        [doi_xung] = @doi_xung,
                        [so_lop] = @so_lop,
                        [so_btp] = @so_btp,
                        [Size_Parameter_Cat] = @Size_Parameter_Cat,
                        [Size_Parameter_CPI] = @Size_Parameter_CPI,
                        [Size_Parameter_TP] = @Size_Parameter_TP,
                        [Remark] = @Remark,
                        [Re_Audit] = @Re_Audit,
                        [UserUpdate] = @UserUpdate,
                        [Photo_URL] = @Photo_URL
                    WHERE ID = @ID";

                using (SqlCommand cmd = new(query, conn))
                {
                    AddBCDTParameters(cmd, request);
                    cmd.Parameters.AddWithValue("@ID", request.ID);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    
                    return rowsAffected > 0 ? "success" : "error";
                }
            }
        }

        private string DeleteBCDT(BCDTModel request)
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
                            INSERT INTO Form3_BCDT_Delete 
                            SELECT *, @DeletedBy, @DeletedTime 
                            FROM Form3_BCDT 
                            WHERE ID = @ID";

                        using (SqlCommand cmdBackup = new(insertBackupQuery, conn, transaction))
                        {
                            cmdBackup.Parameters.AddWithValue("@ID", request.ID);
                            cmdBackup.Parameters.AddWithValue("@DeletedBy", request.UserUpdate ?? "");
                            cmdBackup.Parameters.AddWithValue("@DeletedTime", DateTime.Now);
                            cmdBackup.ExecuteNonQuery();
                        }

                        // Delete bản ghi gốc
                        string deleteQuery = "DELETE FROM Form3_BCDT WHERE ID = @ID";
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
                        _logger.LogError(ex, "Error in DeleteBCDT transaction");
                        return "error: " + ex.Message;
                    }
                }
            }
        }

        private void AddBCDTParameters(SqlCommand cmd, BCDTModel request)
        {
            cmd.Parameters.AddWithValue("@Report_ID", request.Report_ID ?? "");
            cmd.Parameters.AddWithValue("@Unit", request.Unit ?? "");
            cmd.Parameters.AddWithValue("@Cut_Leader", request.Cut_Leader ?? "");
            cmd.Parameters.AddWithValue("@MO", request.MO ?? "");
            cmd.Parameters.AddWithValue("@Color", request.Color?.ToUpper() ?? "");
            cmd.Parameters.AddWithValue("@Roll", request.Roll ?? "");
            cmd.Parameters.AddWithValue("@Lot", request.Lot?.ToUpper() ?? "");
            cmd.Parameters.AddWithValue("@QTY", request.QTY);
            cmd.Parameters.AddWithValue("@Layer_QTY", request.Layer_QTY);
            cmd.Parameters.AddWithValue("@CutTableRatio", request.CutTableRatio ?? "");
            cmd.Parameters.AddWithValue("@Cut_QTY", request.Cut_QTY);
            cmd.Parameters.AddWithValue("@Cut_Table_Height", request.Cut_Table_Height ?? "");
            cmd.Parameters.AddWithValue("@Cut_Table_Long", request.Cut_Table_Long ?? "");
            
            // Các trường boolean (chuyển từ PHP truthy/falsy sang 1/0)
            cmd.Parameters.AddWithValue("@vai_ke", request.vai_ke );
            cmd.Parameters.AddWithValue("@noi_vai", request.noi_vai );
            cmd.Parameters.AddWithValue("@cang_vai", request.cang_vai);
            cmd.Parameters.AddWithValue("@sai_mat", request.sai_mat );
            cmd.Parameters.AddWithValue("@hep_kho", request.hep_kho  );
            cmd.Parameters.AddWithValue("@ban_vai", request.ban_vai  );
            cmd.Parameters.AddWithValue("@vi_tri", request.vi_tri  );
            cmd.Parameters.AddWithValue("@vai_nghieng", request.vai_nghieng  );
            cmd.Parameters.AddWithValue("@song_vai", request.song_vai  );
            cmd.Parameters.AddWithValue("@thang_ke", request.thang_ke  );
            cmd.Parameters.AddWithValue("@khac_mau", request.khac_mau  );
            cmd.Parameters.AddWithValue("@quen_bam", request.quen_bam  );
            cmd.Parameters.AddWithValue("@bam_sau", request.bam_sau  );
            cmd.Parameters.AddWithValue("@xoc_xech", request.xoc_xech  );
            cmd.Parameters.AddWithValue("@khong_cat", request.khong_cat  );
            cmd.Parameters.AddWithValue("@khong_gon", request.khong_gon  );
            cmd.Parameters.AddWithValue("@doi_ke", request.doi_ke  );
            cmd.Parameters.AddWithValue("@doi_xung", request.doi_xung  );
            cmd.Parameters.AddWithValue("@so_lop", request.so_lop  );
            cmd.Parameters.AddWithValue("@so_btp", request.so_btp  );
            
            cmd.Parameters.AddWithValue("@Size_Parameter_Cat", request.Size_Parameter_Cat ?? "");
            cmd.Parameters.AddWithValue("@Size_Parameter_CPI", request.Size_Parameter_CPI ?? "");
            cmd.Parameters.AddWithValue("@Size_Parameter_TP", request.Size_Parameter_TP ?? "");
            cmd.Parameters.AddWithValue("@Remark", request.Remark ?? "");
            cmd.Parameters.AddWithValue("@Re_Audit", request.Re_Audit ? 1 : 0);
            cmd.Parameters.AddWithValue("@Audit_Time", request.Audit_Time);
            cmd.Parameters.AddWithValue("@UserUpdate", request.UserUpdate ?? "");
            cmd.Parameters.AddWithValue("@Photo_URL", request.Photo_URL ?? "");
        }
    }
}