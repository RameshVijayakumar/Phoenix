using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Common;
using Phoenix.DataAccess;
using Phoenix.RuleEngine;

namespace Phoenix.Web.Models
{

    public class AdminService : IAdminService, IUserPermissions
    {
        private IRepository _repository;
        private DbContext _context;
        public List<NetworkPermissions> Permissions { get; set; }

        private AccountService _accountService;

        public static bool IsBrandLevelAdmin { get; set; }
        public static NetworkObjectTypes HighestLevelAccess { get; set; }

        /// <summary>
        /// .ctor
        /// </summary>
        public AdminService()
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _accountService = new AccountService();
        }

        /// <summary>
        /// Load the User Access Permissions by UserId or ProviderUserId (@ login)
        /// </summary>
        /// <param name="username"></param>
        /// <param name="ProviderUserId"></param>
        public string LoadAccess(string username)
        {
                if (string.IsNullOrWhiteSpace(username) == false)
                {
                    var permissions = _accountService.GetUserNetworkAccess(username);

                    if (permissions != null)
                    {
                        Permissions = permissions.ToList();
                    }
                }
                return username;
        }
    }

    public interface IAdminService
    {
        string LoadAccess(string UserId);
    }
}