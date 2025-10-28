using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QOS.Data;
using QOS.Models;

namespace QOS.Services
{
    public class CommonDataService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CommonDataService> _logger;
        private readonly AppSettings _appSettings;
        private readonly AppDbContext _context;


        public CommonDataService(IConfiguration configuration, ILogger<CommonDataService> logger,IOptions<AppSettings> appSettings, AppDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _appSettings = appSettings.Value;
            _context = context;
        }

        public List<string> GetZoneList()
        {
            var zones = new List<string>();
            try
            {
                string connStr = _configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"SELECT DISTINCT Zone
                                   FROM Unit
                                   WHERE Act='Y'
                                   ORDER BY Zone ASC";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                zones.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Zone list");
            }

            return zones;
        }

        public List<Unit_List> GetUnitList()
        {
            try
            {
                var units = _context.Set<Unit_List>()
                    .Where(u => u.Factory == _appSettings.FactoryName)
                    .OrderBy(u => u.Unit)
                    .ToList();

                // _logger.LogInformation($"Loaded {units.Count} units from database");
                return units;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit list");
                return new List<Unit_List>();
            }
        }

    }
}