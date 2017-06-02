using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AAZWeb.Models;
using AAZWeb.Helpers;
using static AAZWeb.Helpers.CatalogModelHelper;

namespace AAZWeb.Controllers
{
    public class CatalogController : Controller
    {
        //
        // GET: /Catalog/

        public ActionResult Index()
        {            
            ViewBag.Title = "Auto Parts Catalog - AutohausAZ";
            RequestObjects ro = GetRequestObjects();
            CatalogModel cm = new CatalogModel();

            ViewBag.SearchByTerm = ro.SearchByTerm;
            ViewBag.Subcategory = ro.Subcategory;
            ViewBag.Vehicle = ro.Vehicle;
            ViewBag.Make = ro.Make;
            ViewBag.Year = ro.Year;
            ViewBag.SearchBy = ro.SearchBy;
            ViewBag.Title = GetCatalogPageTitle(ro);

            // put searchby default or searchby and vehicle session variable code !!! look for clear in querystring to clear session vehicle
            if (RouteData.Values["searchby"] == null) // if search type is missing, redirect to correct search type 
            {
                ro.SearchBy = (CatalogModel.SearchBy)ControllerContext.HttpContext.Session["searchby"];
                Response.Redirect(GetCatalogURL(ro.SearchBy, ro.Make, ro.Year, ro.Vehicle, ro.SearchByTerm, ro.Subcategory));
            }

            if (ro.Part != null)
            {                
                ViewBag.Fitment = cm.GetFitmentVehicle(ro.Part.PartNum, ro.Part.Subcategory.SubcategoryId.ToString(), ro.Vehicle);
                ViewBag.ShowPartailView = "partdetail";
                return View(ro.Part);
            }
            else if (ro.Subcategory != null)
            {
                ViewBag.ShowPartailView = "partlist";
                return View();
            }
            else if (RouteData.Values["searchbyterm"] != null)
            {
                ViewBag.ShowPartailView = "subcategory";
                return View(cm.GetSubcategories(ro.Vehicle, ro.SearchByTerm));
            } 
            else if (RouteData.Values["model"] != null)
            {
                ViewBag.ShowPartailView = "category";
                return View(cm.GetCategories(ro.Vehicle));
            }  
            else if (RouteData.Values["year"] != null)
            {
                ViewBag.ShowPartailView = "model";
                return View(cm.GetModels(ro.Make, ro.Year));
            }  
            else if (RouteData.Values["make"] != null)
            {
                ViewBag.ShowPartailView = "year";
                return View(cm.GetYears(ro.Make));
            }    
            else
            {
                ViewBag.ShowPartailView = "make";
                return View(cm.GetMakes());
            }
        }

        

        private RequestObjects GetRequestObjects(CatalogModel.Vehicle Vehicle = null, CatalogModel.SearchByTerm SearchByTerm = null, CatalogModel.Subcategory Subcategory = null, CatalogModel.SearchBy SearchBy = null, CatalogModel.Make Make = null, CatalogModel.Year Year = null)
        {
            RequestObjects ro = new RequestObjects();
            CatalogModel cm = new CatalogModel();

            ro.Vehicle = Vehicle;
            ro.SearchByTerm = SearchByTerm;
            ro.Subcategory = Subcategory;
            
            ro.SearchBy = SearchBy ?? (RouteData.Values["searchby"] != null ? GetSearchBy(RouteData.Values["searchby"].ToString()): CatalogModel.SearchBy.Category);
            ro.Make = Vehicle?.VehicleMake ?? (Make ?? (RouteData.Values["make"] != null ? new CatalogModel.Make(RouteData.Values["make"].ToString()) : null));
            ro.Year = Vehicle?.VehicleYear ?? (Year ?? (RouteData.Values["year"] != null ? new CatalogModel.Year(RouteData.Values["year"].ToString()) : null));

            if (Vehicle == null && RouteData.Values["model"] != null)
            {
                string modelurl = RouteData.Values["model"].ToString();
                ro.Vehicle = new CatalogModel.Vehicle(modelurl.Split('-')[0]); // get the id part of the model url variable
                if (SearchByTerm == null && RouteData.Values["searchbyterm"] != null)
                {
                    if (ro.SearchBy == CatalogModel.SearchBy.Category)
                        ro.SearchByTerm = new CatalogModel.Category(RouteData.Values["searchbyterm"].ToString().Split('-')[0], ro.Vehicle.isACES);
                    else if (ro.SearchBy == CatalogModel.SearchBy.RepairJob)
                        ro.SearchByTerm = new CatalogModel.RepairJob(RouteData.Values["searchbyterm"].ToString().Split('-')[0]);
                    else if (ro.SearchBy == CatalogModel.SearchBy.PartType)
                        ro.SearchByTerm = new CatalogModel.PartType(RouteData.Values["searchbyterm"].ToString().Split('-')[0]);

                    if (Subcategory == null && RouteData.Values["subcategory"] != null)
                    {
                        String subcategoryurl = RouteData.Values["subcategory"].ToString();
                        ro.Subcategory = new CatalogModel.Subcategory(subcategoryurl.Split('-')[0], ro.Vehicle.isACES, ro.SearchByTerm);
                    }
                }
            }
            if (RouteData.Values["part"] != null)
                ro.Part = cm.GetPart(RouteData.Values["part"].ToString(), ro.Subcategory);

            ControllerContext.HttpContext.Session["selectedvehicle"] = ro.Vehicle; // set session variable for selected vehicle
            ControllerContext.HttpContext.Session["searchby"] = ro.SearchBy; // set session variable for selected vehicle
            ro.page = Request.QueryString["page"] != null ? int.Parse(Request.QueryString["page"]) : 1;
            ro.filterbybrand = string.IsNullOrEmpty(Request.QueryString["filterbybrand"]) ? string.Empty : Request.QueryString["filterbybrand"];
            ro.orderby = string.IsNullOrEmpty(Request.QueryString["filterbybrand"]) ? string.Empty : Request.QueryString["filterbybrand"];
            ro.filterbybrand = HttpUtility.UrlDecode(ro.filterbybrand);
            return ro;
        }

        public PartialViewResult GetLeftBar(CatalogModel.Vehicle Vehicle = null, CatalogModel.SearchByTerm SearchByTerm = null, CatalogModel.Subcategory Subcategory = null)
        {
            return PartialView("_LeftBar");
        }

        public PartialViewResult GetPartList()
        {
            return GetPartListHTMLAction(); // reroute to the existing action call
        }

        public PartialViewResult GetSubcategoryList()
        {
            return GetLeftBarSubcategoryListHTMLAction(); // reroute to the existing action call
        }

        public PartialViewResult GetCategoryList()
        {
            return GetLeftBarCategoryListHTMLAction(); // reroute to the existing action call
        }

        public PartialViewResult GetSearchResults(string keywords = "")
        {
            CatalogModel cm = new CatalogModel();
            List<CatalogModel.Subcategory> SubcategoryList = new List<CatalogModel.Subcategory>();
            String modelurl = RouteData.Values["model"].ToString();
            CatalogModel.Vehicle Vehicle = new CatalogModel.Vehicle(modelurl.Split('-')[0]); // get the id part of the model url variable
            if (!keywords.Trim().Equals(""))
                SubcategoryList = cm.GetSubcategories(Vehicle, keywords);
            ViewBag.Vehicle = Vehicle;
            return PartialView("_SearchResults", SubcategoryList);
        }

        public PartialViewResult GetSelectedVehicleHTMLAction()
        {
            return PartialView("_SelectedVehicle", ControllerContext.HttpContext.Session["selectedvehicle"]);
        }

        public PartialViewResult GetLeftBarHTMLAction(CatalogModel.SearchBy searchby = null, CatalogModel.Vehicle vehicle = null, CatalogModel.SearchByTerm searchbyterm = null, CatalogModel.Subcategory subcategory = null)
        {
            return searchbyterm != null ? GetLeftBarSubcategoryListHTMLAction(searchby, vehicle, searchbyterm, subcategory) : GetLeftBarCategoryListHTMLAction(searchby, vehicle);
        }

        public PartialViewResult GetLeftBarSubcategoryListHTMLAction(CatalogModel.SearchBy searchby = null, CatalogModel.Vehicle vehicle = null, CatalogModel.SearchByTerm searchbyterm = null, CatalogModel.Subcategory subcategory = null)
        {
            CatalogModel cm = new CatalogModel();
            RequestObjects ro = GetRequestObjects(vehicle, searchbyterm, subcategory, searchby);

            ViewBag.SelectedSubcategoryId = ro.Subcategory?.SubcategoryId ?? 0;
            ViewBag.SearchByTerm = ro.SearchByTerm;
            ViewBag.Vehicle = ro.Vehicle;
            ViewBag.SearchType = ro.SearchBy.NamePlural;

            return PartialView("_LeftBarSubcategoryList", cm.GetSubcategories(ro.Vehicle, ro.SearchByTerm));
        }

        public PartialViewResult GetLeftBarCategoryListHTMLAction(CatalogModel.SearchBy searchby = null, CatalogModel.Vehicle vehicle = null)
        {
            CatalogModel cm = new CatalogModel();
            RequestObjects ro = GetRequestObjects(vehicle);

            ViewBag.Vehicle = ro.Vehicle;
            ViewBag.SearchType = ro.SearchBy;

            return PartialView("_LeftBarCategoryList", cm.GetSearbyByTerms(ro.Vehicle, ro.SearchBy));
        }

        public PartialViewResult GetCatalogBreadcrumbsHTMLAction(CatalogModel.SearchBy searchby = null, CatalogModel.Make make = null, CatalogModel.Year year = null, CatalogModel.Vehicle vehicle = null, CatalogModel.SearchByTerm searchbyterm = null, CatalogModel.Subcategory subcategory = null)
        {
            List<CatalogModel.CatalogBreadCrumb> BreadCrumbs = new List<CatalogModel.CatalogBreadCrumb>();
            BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb("Catalog", GetCatalogURL(), "home"));

            CatalogModel cm = new CatalogModel();

            RequestObjects ro = GetRequestObjects(vehicle, searchbyterm, subcategory, searchby, make, year);

            if (ro.Make != null) BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb(ro.Make.MakeName, GetCatalogURL(ro), "make"));
            if (ro.Year != null) BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb(ro.Year.YearId.ToString(), GetCatalogURL(ro), "year"));
            if (ro.Vehicle != null) BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb(ro.Vehicle.VehicleName, GetCatalogURL(ro.SearchBy, ro.Vehicle.VehicleMake, ro.Vehicle.VehicleYear, ro.Vehicle), "vehicle"));
            if (ro.SearchByTerm != null) BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb(ro.SearchByTerm.SearchByTermName, GetCatalogURL(ro.SearchBy, ro.Vehicle.VehicleMake, ro.Vehicle.VehicleYear, ro.Vehicle, ro.SearchByTerm), "category"));
            if (ro.Subcategory != null) BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb(ro.Subcategory.SubcategoryName, GetCatalogURL(ro.SearchBy, ro.Vehicle.VehicleMake, ro.Vehicle.VehicleYear, ro.Vehicle, ro.SearchByTerm, ro.Subcategory), "subcategory"));
            
            return PartialView("_BreadCrumbs", BreadCrumbs);   
        }

        public PartialViewResult GetPartListHTMLAction(CatalogModel.SearchBy searchby = null, CatalogModel.Vehicle vehicle = null, CatalogModel.SearchByTerm searchbyterm = null, CatalogModel.Subcategory subcategory = null)
        {
            CatalogModel cm = new CatalogModel();
            RequestObjects ro = GetRequestObjects(vehicle, searchbyterm, subcategory, searchby);


            IEnumerable<CatalogModel.Part> Parts = cm.GetParts(ro.Vehicle, ro.Subcategory);
            var Brands = new List<string>();
            var BrandsEnum = from brand in Parts.Select(p => p.Brand)
                             orderby brand
                             select brand;


            //from brand in _brandRepository.GetMany(b => b.CompanyId == user.CompanyId)
            //             orderby brand.Name
            //             select brand.Name; Parts.Select(p => p.Brand).Distinct().OrderBy(p => p.);
            Brands.AddRange(BrandsEnum.Distinct());

            ViewBag.ShowPartailView = "partlist";
            ViewBag.SearchByTerm = ro.SearchByTerm;
            ViewBag.Subcategory = ro.Subcategory;
            ViewBag.Vehicle = ro.Vehicle;
            ViewBag.Year = ro.Vehicle.VehicleYear;
            ViewBag.Make = ro.Vehicle.VehicleMake;
            ViewBag.SearchBy = ro.SearchBy;

            

            if (!string.IsNullOrWhiteSpace(ro.filterbybrand))
                Parts = from part in Parts
                        where part.Brand.ToLower().IndexOf(ro.filterbybrand.ToLower()) != -1
                        select part;

            if(Brands.Any(b => b.ToLower().Equals(ro.filterbybrand.ToLower())))
            {
                var SelectedBrandsEnum = from b in Brands 
                    where b.ToLower().Equals(ro.filterbybrand.ToLower())
                    select b;
                List<string> SelectedBrands = SelectedBrandsEnum.ToList();
                ro.filterbybrand = SelectedBrands[0].ToString();
           }

            ViewBag.FilterByBrandList = new SelectList(Brands, ro.filterbybrand);

            if (!string.IsNullOrEmpty(ro.orderby))
            {
                switch (ro.orderby.ToLower())
                {
                    case "brand":
                        Parts = Parts.OrderBy(part => part.Brand).ThenBy(part => part.Description);
                        break;
                    case "description":
                        Parts = Parts.OrderBy(part => part.Description).ThenBy(part => part.Brand);
                        break;
                    case "price - lowest first":
                        Parts = Parts.OrderByDescending(part => part.Price);
                        break;
                    case "price - highest first":
                        Parts = Parts.OrderBy(part => part.Price);
                        break;
                    default:
                        Parts = Parts.OrderBy(part => part.Brand).ThenBy(part => part.Description);
                        break;
                }
            }

            string pageurl = GetCatalogURL(ro) + (Request.QueryString.Count > 0 ? ("?" + GetQueryString(Request.QueryString)) : string.Empty);

            ViewBag.Pagination = new Pagination(ro.page, Parts.Count(), 12, pageurl);

            return PartialView("_PartList", Parts.Skip((ro.page - 1) * 12).Take(12));
        }

        public PartialViewResult GetPagination(Pagination pagination)
        {
            return PartialView("_Pagination", pagination);
        }

        public PartialViewResult GetSelectedVehicle()
        {
            return PartialView("_Fitment");

        }

        [HttpPost]
        public JsonResult GetVehicleInfo(string year, string make, string model, string submodel, string engine, string partnum, string subcategory)
        {
            //CatalogModel cm = new CatalogModel();
            //if (!string.IsNullOrWhiteSpace(engine) && !string.IsNullOrWhiteSpace(submodel) && !string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(make) && !string.IsNullOrWhiteSpace(year))
            //{
            //    CatalogModel.Fitment fitment = cm.GetFitmentVehicle(year, make, HttpUtility.UrlDecode(model), HttpUtility.UrlDecode(submodel), engine, partnum, subcategory);
            //    return new JsonResult { Data = fitment, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            //}
            //else if (!string.IsNullOrWhiteSpace(submodel) && !string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(make) && !string.IsNullOrWhiteSpace(year))
            //{
            //    var engines = cm.GetFitmentEngines(year, make, HttpUtility.UrlDecode(model), HttpUtility.UrlDecode(submodel)).Select(e => new {
            //        text = e.EngineName,
            //        value = e.EngineId
            //    });
            //    return new JsonResult { Data = engines, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            //}
            //else if (!string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(make) && !string.IsNullOrWhiteSpace(year))
            //{
            //    var submodels = cm.GetFitmentSubmodels(year, make, HttpUtility.UrlDecode(model)).Select(s => new {
            //        text = s.SubmodelName,
            //        value = s.SubmodelId
            //    });
            //    return new JsonResult { Data = submodels, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            //}
            //else if (!string.IsNullOrWhiteSpace(make) && !string.IsNullOrWhiteSpace(year))
            //{
            //    var models = cm.GetFitmentModels(year, make).Select(m => new {
            //        text = m.ModelName,
            //        value = m.ModelId
            //    });
            //    return new JsonResult { Data = models, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            //}
            //else if (!string.IsNullOrWhiteSpace(year))
            //{
            //    var makes = cm.GetFitmentMakes(year).Select(m => new {
            //        text = m.MakeName,
            //        value = m.MakeId
            //    });
            //    return new JsonResult { Data = makes, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            //}
            //else
            //{
            //    var years = cm.GetFitmentYears().Select(y => new {
            //        text = y.YearId,
            //        value = y.YearId
            //    });
            //    return new JsonResult { Data = years, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            //}
            return null;
        }

        public PartialViewResult SearchBox()
        {
            return PartialView("_CatalogSearch");
        }

        [HttpPost]
        public JsonResult GetVehicle(string year, string make, string model, string submodel, string engine)
        {
            CatalogModel cm = new CatalogModel();
            CatalogModel.Vehicle vehicle = cm.GetVehicle(year, make, model, submodel, engine);
            return new JsonResult { Data = vehicle, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}
