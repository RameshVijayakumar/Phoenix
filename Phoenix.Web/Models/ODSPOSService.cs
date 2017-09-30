using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Common;
using Phoenix.DataAccess;
using Phoenix.Web.Models.Grid;
using System.Linq.Dynamic;

namespace Phoenix.Web.Models
{
    public class ODSPOSService : KendoGrid<ODSPOSData>, IODSPOSService
    {
        private IRepository _repository;
        private DbContext _context;
        private IRepository _odsRepository;
        private DbContext _odsContext;
        public int Count { get; set; }


        /// <summary>
        /// .ctor
        /// </summary>
        public ODSPOSService()
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _odsContext = new PhoenixODSContext();
            _odsRepository = new GenericRepository(_odsContext);
        }

        /// <summary>
        /// return site list to view model
        /// </summary>
        /// <param name="netId">selected NetworkObject</param>
        /// <returns>site lsit</returns>
        public List<ODSPOSData> GetODSPOSDataList(ODSPOSGridRequest grdRequest)
        {
            List<ODSPOSData> siteList = null;
            try
            {
                var filtering = GetFiltering(grdRequest);
                var sorting = GetSorting(grdRequest);

                //Allow for full inquiry
                if (grdRequest.networkObjectId == null)
                {
                    var qryList = _odsRepository.GetAll<ODSPOSData>().AsQueryable()
                                    .Where(filtering)
                                    .OrderBy(sorting);

                    Count = qryList.Count();
                    siteList = qryList.Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList();
                }
                else
                {
                    var site = _repository.GetQuery<SiteInfo>(si => si.NetworkObjectId == grdRequest.networkObjectId).FirstOrDefault();
                    var siteIrisId = site == null ? 0 : site.NetworkObject.IrisId;

                    if (siteIrisId != 0)
                    {
                        var qryList = (from odsp in _odsRepository.GetQuery<ODSPOSData>().AsQueryable()
                                       where odsp.IrisId == siteIrisId
                                       select odsp).AsQueryable();
                        qryList = qryList.Where(filtering)
                                         .OrderBy(sorting);


                        Count = qryList.Count();
                        siteList = qryList.Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return siteList;
        }
    }

    public interface IODSPOSService
    {
        int Count { get; set; }
        List<ODSPOSData> GetODSPOSDataList(ODSPOSGridRequest grdRequest);
    }
}