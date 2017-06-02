using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AAZWeb.Models;
using AAZWeb.Helpers;

namespace AAZWeb.Controllers
{
    public class UniversalController : Controller
    {
        //
        // GET: /Catalog/

        public ActionResult Index()
        {
            UniversalCatalogModel cm = new UniversalCatalogModel();
            ViewBag.Title = "Car Care, Tools & Accessories - AutohausAZ";

            RequestObjects ro = GetRequestObjects();
            ViewBag.Category = ro.Category;
            ViewBag.Subcategory = ro.Subcategory;
            ViewBag.Brand = ro.Brand;

            if (ViewBag.Category == null && ViewBag.Subcategory == null && ViewBag.Brand == null)
            {
                ViewBag.ShowPartialView = "category";
                ViewBag.FeaturedBrands = cm.GetFeaturedUniversalBrands();
                return View(cm.GetUniversalCategories());
            }
            else
            {
                ViewBag.ShowPartialView = "partlist";
            }
            return View();
        }

        public PartialViewResult GetFeaturedUniversalBrands(List<UniversalCatalogModel.UniversalFeaturedBrand> Brands)
        {
            return PartialView("_FeaturedUniversalBrands", Brands);
        }

        public PartialViewResult GetUniversalPartList()
        {
            return GetUniversalPartListHTMLAction();
        }

        public PartialViewResult GetUniversalPartListHTMLAction(UniversalCatalogModel.UniversalFeaturedBrand brand = null, UniversalCatalogModel.UniversalCategory category = null, CatalogModel.Subcategory subcategory = null)
        {
            UniversalCatalogModel cm = new UniversalCatalogModel();

            RequestObjects ro = GetRequestObjects(brand, category, subcategory);     

            IEnumerable<CatalogModel.Part> Parts = cm.GetUniversalParts(ro.Brand, ro.Category, ro.Subcategory).OrderBy(part => part.Brand).ThenBy(part => part.Description);
            var Brands = new List<string>();
            var BrandsEnum = from brandname in Parts.Select(p => p.Brand)
                             orderby brandname
                             select brandname;

            Brands.AddRange(BrandsEnum.Distinct());

            ViewBag.ShowPartailView = "partlist";
            ViewBag.Category = ro.Category;
            ViewBag.Subcategory = ro.Subcategory;
            ViewBag.Brand = ro.Brand;

            string filterbybrand = string.IsNullOrEmpty(Request.QueryString["filterbybrand"]) ? string.Empty : Request.QueryString["filterbybrand"];
            string orderby = string.IsNullOrEmpty(Request.QueryString["filterbybrand"]) ? string.Empty : Request.QueryString["filterbybrand"];
            filterbybrand = HttpUtility.UrlDecode(filterbybrand);

            if (!string.IsNullOrWhiteSpace(filterbybrand))
                Parts = from part in Parts
                        where part.Brand.ToLower().IndexOf(filterbybrand.ToLower()) != -1
                        select part;

            if (Brands.Any(b => b.ToLower().Equals(filterbybrand.ToLower())))
            {
                var SelectedBrandsEnum = from b in Brands
                                         where b.ToLower().Equals(filterbybrand.ToLower())
                                         select b;
                List<string> SelectedBrands = SelectedBrandsEnum.ToList();
                filterbybrand = SelectedBrands[0].ToString();
            }

            ViewBag.FilterByBrandList = new SelectList(Brands, filterbybrand);

            if (!string.IsNullOrEmpty(orderby))
                orderby = String.Empty;

            switch (orderby.ToLower())
            {
                case "brand":
                    Parts.OrderBy(part => part.Brand).ThenBy(part => part.Description);
                    break;
                case "description":
                    Parts.OrderBy(part => part.Description).ThenBy(part => part.Brand);
                    break;
                case "price - lowest first":
                    Parts.OrderByDescending(part => part.Price);
                    break;
                case "price - highest first":
                    Parts.OrderBy(part => part.Price);
                    break;
                default: // by brand 
                    Parts.OrderBy(part => part.Brand).ThenBy(part => part.Description);
                    break;
            }

            string pageurl = CatalogModelHelper.GetUniversalCatalogURL(ro.Brand, ro.Category, ro.Subcategory) + "?" + CatalogModelHelper.GetQueryString(Request.QueryString);

            ViewBag.Pagination = new CatalogModelHelper.Pagination(ro.page, Parts.Count(), 12, pageurl);

            return PartialView("~/Views/Catalog/_PartList.cshtml", Parts.Skip((ro.page -1) * 12).Take(12));
        }

        public PartialViewResult GetCatalogBreadcrumbsHTMLAction(UniversalCatalogModel.UniversalFeaturedBrand Brand = null, UniversalCatalogModel.UniversalCategory Category = null, CatalogModel.Subcategory Subcategory = null)
        {
            List<CatalogModel.CatalogBreadCrumb> BreadCrumbs = new List<CatalogModel.CatalogBreadCrumb>();
            BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb("Catalog", CatalogModelHelper.GetUniversalCatalogURL(), "home"));
            RequestObjects ro = GetRequestObjects(Brand, Category, Subcategory);            
            if (ro.Brand != null) BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb(Brand.BrandName, CatalogModelHelper.GetUniversalCatalogURL(Brand, null, null), "brand"));
            if (ro.Category != null) BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb(Category.CategoryName, CatalogModelHelper.GetUniversalCatalogURL(Brand, Category, null), "category"));
            if (ro.Subcategory != null) BreadCrumbs.Add(new CatalogModel.CatalogBreadCrumb(Subcategory.SubcategoryName, CatalogModelHelper.GetUniversalCatalogURL(Brand, Category, Subcategory), "subcategory"));

            return PartialView("~/Views/Catalog/_Breadcrumbs.cshtml", BreadCrumbs);
        }

        private RequestObjects GetRequestObjects(UniversalCatalogModel.UniversalFeaturedBrand Brand = null, UniversalCatalogModel.UniversalCategory Category = null, CatalogModel.Subcategory Subcategory = null)
        {
            RequestObjects ro = new RequestObjects();
            UniversalCatalogModel cm = new UniversalCatalogModel();
            if (Brand == null && RouteData.Values["brand"] != null)
            {
                String brandurl = RouteData.Values["brand"].ToString();
                ro.Brand = cm.GetFeaturedUniversalBrand(brandurl);
            }
            else
                ro.Brand = Brand;
            if (Category == null && RouteData.Values["category"] != null)
            {
                String categoryurl = RouteData.Values["category"].ToString();
                ro.Category = new UniversalCatalogModel.UniversalCategory(categoryurl.Split('-')[0]);
            }
            else
                ro.Category = Category;
            if (Subcategory == null && RouteData.Values["subcategory"] != null)
            {
                String subcategoryurl = RouteData.Values["subcategory"].ToString();
                ro.Subcategory = new CatalogModel.Subcategory(RouteData.Values["subcategory"].ToString().Split('-')[0], true, Category);
            }
            else
                ro.Subcategory = Subcategory;
            ro.page = Request.QueryString["page"] != null ? int.Parse(Request.QueryString["page"]) : 1;

            return ro;
        }

        private class RequestObjects
        {
            public CatalogModel.Subcategory Subcategory { get; set; }
            public UniversalCatalogModel.UniversalCategory Category { get; set; }
            public UniversalCatalogModel.UniversalFeaturedBrand Brand { get; set; }
            public int page { get; set; }
        }

        public PartialViewResult GetUniversalSubcategoryList()
        {
            return GetLeftBarUniversalSubcategoryListHTMLAction();
        }

        public PartialViewResult GetUniversalCategoryList()
        {
            return GetLeftBarUniversalCategoryListHTMLAction();
        }

        public PartialViewResult GetLeftBarUniversalSubcategoryListHTMLAction(UniversalCatalogModel.UniversalFeaturedBrand Brand = null, UniversalCatalogModel.UniversalCategory Category = null, CatalogModel.Subcategory Subcategory = null)
        {
            UniversalCatalogModel cm = new UniversalCatalogModel();
            RequestObjects ro = GetRequestObjects(Brand, Category, Subcategory);        

            ViewBag.Brand = ro.Brand;
            ViewBag.Category = ro.Category;
            ViewBag.Subcategory = ro.Subcategory;
            
            return PartialView("_UniversalLeftBarSubcategoryList", cm.GetUniversalSubcategories(ro.Brand, ro.Category));
        }

        public PartialViewResult GetLeftBarUniversalCategoryListHTMLAction(UniversalCatalogModel.UniversalFeaturedBrand Brand = null, UniversalCatalogModel.UniversalCategory Category = null, CatalogModel.Subcategory Subcategory = null)
        {
            UniversalCatalogModel cm = new UniversalCatalogModel();
            RequestObjects ro = GetRequestObjects(Brand, Category, Subcategory);

            ViewBag.Brand = ro.Brand;
            ViewBag.Category = ro.Category;
            ViewBag.Subcategory = ro.Subcategory;

            return PartialView("_UniversalLeftBarCategoryList", cm.GetUniversalCategories(ro.Brand));
        }
    }
}
