//using Phoenix.Common;
//using Phoenix.DataAccess;
//using Phoenix.RuleEngine;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Net;
//using System.Web;

//namespace Phoenix.API.Models
//{
//    public class POSMapService
//    {
//        //private CommonPOSMapService _commonPOSMapService;
//        private IRepository _repository;
//        private DbContext _context;
//        private IRepository _odsRepository;
//        private DbContext _odsContext;

//        public POSMapService()
//        {
//            _context = new ProductMasterContext();
//            _repository = new GenericRepository(_context);

//            _odsContext = new PhoenixODSContext();
//            _odsRepository = new GenericRepository(_odsContext);
//            //_commonPOSMapService = new CommonPOSMapService(_context, _repository, _odsContext, _odsRepository,RuleService.CallType.API);
//        }

//        public string AutoMapPOSByPLU(string siteId, out HttpStatusCode status)
//        {
//            bool retVal = false;
//            var statusMsg = string.Empty;
//            status = HttpStatusCode.OK;
//            try
//            {
//                // Code moved to commonPOSMapService
//                retVal = _commonPOSMapService.AutoMapPOSByPLU(siteId);
//                statusMsg = _commonPOSMapService.LastActionResult;
//                Logger.WriteInfo(_commonPOSMapService.LastActionResult);
//                if(retVal == false)
//                {
//                    status = HttpStatusCode.NotFound;
//                }
//            }
//            catch (Exception ex)
//            {
//                status = HttpStatusCode.InternalServerError;
//                statusMsg = "Unexpected error occured.";
//                Logger.WriteError(ex);
//            }
//            return statusMsg;
//        }
//    }
//}