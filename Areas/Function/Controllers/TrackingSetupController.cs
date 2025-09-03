using System.Data;
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
        public async Task<IActionResult> SaveAction(TrackingSetup dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ModuleName))
                return Json(new { success = false, message = "ModuleName không hợp lệ" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var moduleName = dto.ModuleName;

                // 🔹 Kiểm tra tồn tại trong TRACKING_Module
                var exists = await _context.TrackingSetup
                    .AnyAsync(x => x.ModuleName == moduleName);

                if (!exists)
                {
                    // INSERT vào bảng chính
                    dto.LastUpdate = DateTime.Now;
                    dto.UserUpdate = User.Identity?.Name ?? "System";

                    _context.TrackingSetup.Add(dto);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // UPDATE bảng chính
                    var sqlUpdate = @"
                UPDATE TRACKING_Module
                SET Remark = @p0, UpdateForm_StartRow = @p1, 
                    UserUpdate = @p2, LastUpdate = GETDATE()
                WHERE ModuleName = @p3";

                    await _context.Database.ExecuteSqlRawAsync(sqlUpdate,
                        dto.Remark ?? "",
                        dto.UpdateForm_StartRow,
                        User.Identity?.Name ?? "System",
                        moduleName);
                }

                // 🔹 Lưu bảng InforSetup
                if (dto.InforSetups != null)
                {
                    foreach (var info in dto.InforSetups)
                    {
                        var colName = $"Infor_{info.Index:D2}";
                        var sql = $@"
                    UPDATE TRACKING_InforSetup_Name 
                    SET {colName} = @p0 
                    WHERE ModuleName = @p1;

                    IF @@ROWCOUNT = 0
                        INSERT INTO TRACKING_InforSetup_Name(ModuleName, {colName}) 
                        VALUES(@p1, @p0);";

                        await _context.Database.ExecuteSqlRawAsync(sql, info.Name ?? "", moduleName);
                    }
                }

                // 🔹 Lưu bảng ResultSetup
                if (dto.ResultSetups != null)
                {
                    foreach (var result in dto.ResultSetups)
                    {
                        var colName = $"Result_{result.Index:D2}";
                        var sql = $@"
                    UPDATE TRACKING_ResultSetup_Name 
                    SET {colName} = @p0 
                    WHERE ModuleName = @p1;

                    IF @@ROWCOUNT = 0
                        INSERT INTO TRACKING_ResultSetup_Name(ModuleName, {colName}) 
                        VALUES(@p1, @p0);";

                        await _context.Database.ExecuteSqlRawAsync(sql, result.Name ?? "", moduleName);
                    }
                }

                await transaction.CommitAsync();
                return Json(new { success = true, message = "Lưu thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }



    }
}