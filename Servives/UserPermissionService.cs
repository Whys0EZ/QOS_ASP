using System;
using System.Reflection;
using System.Linq;
using QOS.Data; // DbContext
using QOS.Models; // UserPermission model
using Microsoft.EntityFrameworkCore;

namespace QOS.Services
{
    public class UserPermissionService : IUserPermissionService
    {
        private readonly AppDbContext _context;

        public UserPermissionService(AppDbContext context)
        {
            _context = context;
        }

        public bool HasPermission(string? username, string functionCode)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var user = _context.UserPermissions
                .AsNoTracking()
                .FirstOrDefault(u => u.UserName == username);

            if (user == null)
                return false;

            var property = typeof(UserPermission).GetProperty(
                functionCode,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null || property.PropertyType != typeof(bool))
                return false;

            var value = property.GetValue(user);
            return value is bool b && b;
        }
    }
}
