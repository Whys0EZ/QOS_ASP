using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QOS.Areas.Function.Models;
using QOS.Data;
using QOS.Helpers;


namespace QOS.Areas.Function.Controllers
{
    [Area("Function")]
    [Authorize]
    public class TrackingSetupController : Controller
    {
        private readonly ILogger<TrackingSetupController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public TrackingSetupController(ILogger<TrackingSetupController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";

        public IActionResult Index()
        {
            var setuplist = _context.TrackingSetup.OrderBy(c => c.ModuleName).ToList();
            return View(setuplist);
        }
        // Lấy chi tiết để popup
        [HttpGet]
        public async Task<IActionResult> GetSetup(string moduleName)
        {
            var setup = await _context.TrackingSetup.FirstOrDefaultAsync(x => x.ModuleName == moduleName);
            if (setup == null) return NotFound();



            // Đảm bảo list không null
            setup.InforSetups ??= new List<InforSetupDto>();
            setup.ResultSetups ??= new List<ResultSetupDto>();
            using (var conn = _context.Database.GetDbConnection())
            {
                await conn.OpenAsync();
                // --- Call InforSetup ---
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "Get_TRACKING_InforSetup";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var param = cmd.CreateParameter();
                    param.ParameterName = "@ModuleName";
                    param.Value = moduleName;
                    cmd.Parameters.Add(param);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            setup.InforSetups.Add(new InforSetupDto
                            {
                                SqlColumn = reader["InforName"]?.ToString(),
                                Name = reader["Inf_Name"]?.ToString(),
                                Index = reader["Inf_Index"]?.ToString(),
                                DataType = reader["Inf_DataType"]?.ToString(),
                                Opt = reader["Inf_Opt"]?.ToString(),
                                Column = reader["Inf_Column"]?.ToString(),
                                Remark = reader["Inf_Remark"]?.ToString()
                            });
                        }
                    }
                }

                // --- Call ResultSetup ---
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "Get_TRACKING_InforSetup_Result";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var param = cmd.CreateParameter();
                    param.ParameterName = "@ModuleName";
                    param.Value = moduleName;
                    cmd.Parameters.Add(param);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            setup.ResultSetups.Add(new ResultSetupDto
                            {
                                SqlColumn = reader["InforName"]?.ToString(),
                                Name = reader["Inf_Name"]?.ToString(),
                                Index = reader["Inf_Index"]?.ToString(),
                                DataType = reader["Inf_DataType"]?.ToString(),
                                SelectionData = reader["Inf_SelectionData"]?.ToString(),
                                Remark = reader["Inf_Remark"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(setup);
        }

        public async Task<IActionResult> Create(string? moduleName)
        {
            var setup = await _context.TrackingSetup.FirstOrDefaultAsync(x => x.ModuleName == moduleName)
            ?? new TrackingSetup
            {
                ModuleName = moduleName ?? "",
                Remark = "",
                UpdateForm_StartRow = 0,
                UserUpdate = "",
                LastUpdate = DateTime.Now
            };
            // if (setup == null) return NotFound();

            setup.InforSetups ??= new List<InforSetupDto>();
            setup.ResultSetups ??= new List<ResultSetupDto>();
            using (var conn = _context.Database.GetDbConnection())
            {
                await conn.OpenAsync();
                // --- Call InforSetup ---
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "Get_TRACKING_InforSetup";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var param = cmd.CreateParameter();
                    param.ParameterName = "@ModuleName";
                    param.Value = moduleName ?? "";
                    cmd.Parameters.Add(param);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            setup.InforSetups.Add(new InforSetupDto
                            {
                                SqlColumn = reader["InforName"]?.ToString(),
                                Name = reader["Inf_Name"]?.ToString(),
                                Index = reader["Inf_Index"]?.ToString(),
                                DataType = reader["Inf_DataType"]?.ToString(),
                                Opt = reader["Inf_Opt"]?.ToString(),
                                Column = reader["Inf_Column"]?.ToString(),
                                Remark = reader["Inf_Remark"]?.ToString()
                            });
                        }
                    }
                }

                // --- Call ResultSetup ---
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "Get_TRACKING_InforSetup_Result";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var param = cmd.CreateParameter();
                    param.ParameterName = "@ModuleName";
                    param.Value = moduleName ?? "";
                    cmd.Parameters.Add(param);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            setup.ResultSetups.Add(new ResultSetupDto
                            {
                                SqlColumn = reader["InforName"]?.ToString(),
                                Name = reader["Inf_Name"]?.ToString(),
                                Index = reader["Inf_Index"]?.ToString(),
                                DataType = reader["Inf_DataType"]?.ToString(),
                                SelectionData = reader["Inf_SelectionData"]?.ToString(),
                                Remark = reader["Inf_Remark"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(setup);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string moduleName)
        {
            using (var conn = _context.Database.GetDbConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE_TRACKING_Setup_ALL";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var param = cmd.CreateParameter();
                    param.ParameterName = "@ModuleName";
                    param.Value = moduleName;
                    cmd.Parameters.Add(param);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return Json(new { success = true, message = "Xóa thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] TrackingSetupSaveDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ModuleName))
                return BadRequest(new { success = false, message = "ModuleName không được rỗng." });

            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lưu bảng chính (TRACKING_Module)
                var module = await _context.Set<TrackingSetup>()
                    .FirstOrDefaultAsync(x => x.ModuleName == dto.ModuleName);

                if (module == null)
                {
                    module = new TrackingSetup
                    {
                        ModuleName = dto.ModuleName,
                        UserUpdate = User.Identity?.Name ?? "system",
                        LastUpdate = DateTime.Now
                    };
                    _context.Add(module);
                }

                module.Remark = dto.Remark;
                module.UpdateForm_StartRow = dto.UpdateForm_StartRow;
                module.UserUpdate = User.Identity?.Name ?? "system";
                module.LastUpdate = DateTime.Now;

                // 2. Chuẩn bị dữ liệu InforSetups (15 cột)
                var inforName = await _context.Set<TRACKING_InforSetup_Name>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_InforSetup_Name { ModuleName = dto.ModuleName };
                var inforIndex = await _context.Set<TRACKING_InforSetup_Index>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_InforSetup_Index { ModuleName = dto.ModuleName };
                var inforType = await _context.Set<TRACKING_InforSetup_DataType>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_InforSetup_DataType { ModuleName = dto.ModuleName };
                var inforOpt = await _context.Set<TRACKING_InforSetup_Opt>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_InforSetup_Opt { ModuleName = dto.ModuleName };
                var inforRemark = await _context.Set<TRACKING_InforSetup_Remark>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_InforSetup_Remark { ModuleName = dto.ModuleName };
                var inforColumn = await _context.Set<TRACKING_InforSetup_Column>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_InforSetup_Column { ModuleName = dto.ModuleName };

                for (int i = 0; i < dto.InforSetups.Count; i++)
                {
                    var item = dto.InforSetups[i];
                    var propName = $"Infor_{(i + 1).ToString("D2")}";

                    inforName.GetType().GetProperty(propName)?.SetValue(inforName, item.Name);
                    inforIndex.GetType().GetProperty(propName)?.SetValue(inforIndex, item.Index);
                    inforType.GetType().GetProperty(propName)?.SetValue(inforType, item.DataType);
                    inforOpt.GetType().GetProperty(propName)?.SetValue(inforOpt, item.Opt);
                    inforRemark.GetType().GetProperty(propName)?.SetValue(inforRemark, item.Remark);
                    inforColumn.GetType().GetProperty(propName)?.SetValue(inforColumn, item.Column);
                }

                if (await _context.Set<TRACKING_InforSetup_Name>().AnyAsync(x => x.ModuleName == dto.ModuleName))
                {
                    _context.Update(inforName);
                    _context.Update(inforIndex);
                    _context.Update(inforType);
                    _context.Update(inforOpt);
                    _context.Update(inforRemark);
                    _context.Update(inforColumn);
                }
                else
                {
                    _context.Add(inforName);
                    _context.Add(inforIndex);
                    _context.Add(inforType);
                    _context.Add(inforOpt);
                    _context.Add(inforRemark);
                    _context.Add(inforColumn);
                }

                // 3. Chuẩn bị dữ liệu ResultSetups (5 cột)
                var resultName = await _context.Set<TRACKING_ResultSetup_Name>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_ResultSetup_Name { ModuleName = dto.ModuleName };
                var resultIndex = await _context.Set<TRACKING_ResultSetup_Index>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_ResultSetup_Index { ModuleName = dto.ModuleName };
                var resultType = await _context.Set<TRACKING_ResultSetup_DataType>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_ResultSetup_DataType { ModuleName = dto.ModuleName };
                var resultRemark = await _context.Set<TRACKING_ResultSetup_Remark>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_ResultSetup_Remark { ModuleName = dto.ModuleName };
                var resultSel = await _context.Set<TRACKING_ResultSetup_SelectionData>()
                    .FindAsync(dto.ModuleName) ?? new TRACKING_ResultSetup_SelectionData { ModuleName = dto.ModuleName };

                for (int i = 0; i < dto.ResultSetups.Count; i++)
                {
                    var item = dto.ResultSetups[i];
                    var propName = $"Infor_{(i + 1).ToString("D2")}";

                    resultName.GetType().GetProperty(propName)?.SetValue(resultName, item.Name);
                    resultIndex.GetType().GetProperty(propName)?.SetValue(resultIndex, item.Index);
                    resultType.GetType().GetProperty(propName)?.SetValue(resultType, item.DataType);
                    resultRemark.GetType().GetProperty(propName)?.SetValue(resultRemark, item.Remark);
                    resultSel.GetType().GetProperty(propName)?.SetValue(resultSel, item.SelectionData);
                }

                if (await _context.Set<TRACKING_ResultSetup_Name>().AnyAsync(x => x.ModuleName == dto.ModuleName))
                {
                    _context.Update(resultName);
                    _context.Update(resultIndex);
                    _context.Update(resultType);
                    _context.Update(resultRemark);
                    _context.Update(resultSel);
                }
                else
                {
                    _context.Add(resultName);
                    _context.Add(resultIndex);
                    _context.Add(resultType);
                    _context.Add(resultRemark);
                    _context.Add(resultSel);
                }

                // 4. Save & Commit
                // await _context.SaveChangesAsync();
                try
                {
                    var affected = await _context.SaveChangesAsync();
                    Console.WriteLine($"Rows affected: {affected}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                // await tran.CommitAsync();

                return Json(new { success = true, message = "Lưu thành công" });
            }
            catch (Exception ex)
            {
                // await tran.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveAction([FromBody] TrackingSetupSaveDto dto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var tran = conn.BeginTransaction();

            try
            {
                var now = DateTime.Now;
                var user = User.Identity?.Name ??"system"; // TODO: thay bằng User.Identity.Name

                // 1. Insert/Update TRACKING_Module
                var cmd = conn.CreateCommand();
                cmd.Transaction = tran;

                // Kiểm tra tồn tại
                cmd.CommandText = "SELECT COUNT(*) FROM TRACKING_Module WHERE ModuleName=@ModuleName";
                var pModule = cmd.CreateParameter();
                pModule.ParameterName = "@ModuleName";
                pModule.Value = dto.ModuleName;
                cmd.Parameters.Add(pModule);

                var exists = (int)(await cmd.ExecuteScalarAsync() ?? 0);

                if (exists > 0)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"UPDATE TRACKING_Module 
                                SET Remark=@Remark, UpdateForm_StartRow=@StartRow,
                                    UserUpdate=@UserUpdate, LastUpdate=@LastUpdate
                                WHERE ModuleName=@ModuleName";
                    cmd.Parameters.Add(new SqlParameter("@Remark", dto.Remark ?? (object)DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@StartRow", (object?)dto.UpdateForm_StartRow ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@UserUpdate", user));
                    cmd.Parameters.Add(new SqlParameter("@LastUpdate", now));
                    cmd.Parameters.Add(new SqlParameter("@ModuleName", dto.ModuleName));
                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"INSERT INTO TRACKING_Module(ModuleName, Remark, UpdateForm_StartRow, UserUpdate, LastUpdate)
                                VALUES(@ModuleName, @Remark, @StartRow, @UserUpdate, @LastUpdate)";
                    cmd.Parameters.Add(new SqlParameter("@ModuleName", dto.ModuleName));
                    cmd.Parameters.Add(new SqlParameter("@Remark", dto.Remark ?? (object)DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@StartRow", (object?)dto.UpdateForm_StartRow ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@UserUpdate", user));
                    cmd.Parameters.Add(new SqlParameter("@LastUpdate", now));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Lưu InforSetups vào 6 bảng
                await SaveInforTables(conn, tran, dto.ModuleName, dto.InforSetups);

                // 3. Lưu ResultSetups vào 5 bảng
                await SaveResultTables(conn, tran, dto.ModuleName, dto.ResultSetups);

                tran.Commit();
                return Json(new { success = true, message = "Lưu thành công!" });
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return Json(new { success = false, message = "Lỗi khi lưu: " + ex.Message });
            }
        }
        private async Task SaveInforTables(DbConnection conn, DbTransaction tran, string moduleName, List<InforSetupDto> list)
        {
            string[] tables = {
                            "TRACKING_InforSetup_Name",
                            "TRACKING_InforSetup_Index",
                            "TRACKING_InforSetup_DataType",
                            "TRACKING_InforSetup_Remark",
                            "TRACKING_InforSetup_Opt",
                            "TRACKING_InforSetup_Column"
                            };

            foreach (var tbl in tables)
            {
                var cmd = conn.CreateCommand();
                cmd.Transaction = tran;

                // Kiểm tra đã có chưa
                cmd.CommandText = $"SELECT COUNT(*) FROM {tbl} WHERE ModuleName=@ModuleName";
                cmd.Parameters.Add(new SqlParameter("@ModuleName", moduleName));
                var exists = (int)(await cmd.ExecuteScalarAsync() ?? 0);

                // Build SQL động
                var cols = new List<string>();
                var vals = new List<string>();
                var sets = new List<string>();

                for (int i = 0; i < list.Count; i++)
                {
                    string colName = $"Infor_{(i + 1).ToString("D2")}";
                    string paramName = $"@p{i}";

                    cols.Add(colName);
                    vals.Add(paramName);
                    sets.Add($"{colName}={paramName}");

                    #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    string value = tbl switch
                    {
                        "TRACKING_InforSetup_Name" => list[i].Name,
                        "TRACKING_InforSetup_Index" => list[i].Index,
                        "TRACKING_InforSetup_DataType" => list[i].DataType,
                        "TRACKING_InforSetup_Remark" => list[i].Remark,
                        "TRACKING_InforSetup_Opt" => list[i].Opt,
                        "TRACKING_InforSetup_Column" => list[i].Column,
                        _ => ""
                    };
                    #pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                    cmd.Parameters.Add(new SqlParameter(paramName, (object?)value ?? DBNull.Value));
                }

                if (exists > 0)
                {
                    cmd.CommandText = $"UPDATE {tbl} SET {string.Join(",", sets)} WHERE ModuleName=@ModuleName";
                }
                else
                {
                    cmd.CommandText = $"INSERT INTO {tbl}(ModuleName,{string.Join(",", cols)}) VALUES(@ModuleName,{string.Join(",", vals)})";
                }

                await cmd.ExecuteNonQueryAsync();
            }
        }
        private async Task SaveResultTables(DbConnection conn, DbTransaction tran, string moduleName, List<ResultSetupDto> list)
        {
            string[] tables = {
                                "TRACKINIG_ResultSetup_Name",
                                "TRACKINIG_ResultSetup_Index",
                                "TRACKINIG_ResultSetup_DataType",
                                "TRACKINIG_ResultSetup_SelectionData",
                                "TRACKINIG_ResultSetup_Remark"
                            };

            foreach (var tbl in tables)
            {
                var cmd = conn.CreateCommand();
                cmd.Transaction = tran;

                cmd.CommandText = $"SELECT COUNT(*) FROM {tbl} WHERE ModuleName=@ModuleName";
                cmd.Parameters.Add(new SqlParameter("@ModuleName", moduleName));
                var exists = (int)(await cmd.ExecuteScalarAsync() ?? 0);

                var cols = new List<string>();
                var vals = new List<string>();
                var sets = new List<string>();

                for (int i = 0; i < list.Count; i++)
                {
                    string colName = $"Infor_{(i + 1).ToString("D2")}";
                    string paramName = $"@p{i}";

                    cols.Add(colName);
                    vals.Add(paramName);
                    sets.Add($"{colName}={paramName}");

                    #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    string value = tbl switch
                    {
                        "TRACKINIG_ResultSetup_Name" => list[i].Name,
                        "TRACKINIG_ResultSetup_Index" => list[i].Index,
                        "TRACKINIG_ResultSetup_DataType" => list[i].DataType,
                        "TRACKINIG_ResultSetup_SelectionData" => list[i].SelectionData,
                        "TRACKINIG_ResultSetup_Remark" => list[i].Remark,
                        _ => ""
                    };
                    #pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                    cmd.Parameters.Add(new SqlParameter(paramName, (object?)value ?? DBNull.Value));
                }

                if (exists > 0)
                {
                    cmd.CommandText = $"UPDATE {tbl} SET {string.Join(",", sets)} WHERE ModuleName=@ModuleName";
                }
                else
                {
                    cmd.CommandText = $"INSERT INTO {tbl}(ModuleName,{string.Join(",", cols)}) VALUES(@ModuleName,{string.Join(",", vals)})";
                }

                await cmd.ExecuteNonQueryAsync();
            }
        }





    }
}