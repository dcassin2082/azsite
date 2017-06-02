using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using AAZWeb.Models;

namespace AAZWeb.Controllers
{
    public class ImagesController : Controller
    {
        private const string imc_imagepath = "http://mmm56784.blob.core.windows.net/images/";
        private const string az_imagepath = "http://www.autohausaz.com/secure/partimages/";
        private const string az_noimageurl = "http://www.autohausaz.com/secure/partimages/noimage.jpg";

        public ActionResult DisplayImage()
        {
            string partnum = RouteData.Values["partnum"].ToString();
            var filename = partnum + ".jpg";
            Regex r = new Regex(@"\bSKU(\d){8}-(?<partnum>\S+)");
            Match m = r.Match(partnum);
            if(m.Success)
                filename = m.Groups["partnum"].Value + ".jpg";
                       
            byte[] rawimage = null;

            if (ImagesModel.checkImage(az_imagepath + filename)) {
                rawimage = ImagesModel.getImage(az_imagepath + filename);
                Response.StatusCode= 200;
            }
            else if (ImagesModel.checkImage(imc_imagepath + filename)){
                rawimage = ImagesModel.getImage(imc_imagepath + filename);
                Response.StatusCode= 200;
            }
            else{
                rawimage = ImagesModel.getImage(az_noimageurl);
                Response.StatusCode= 404;
            }
                
            return File(rawimage, MimeMapping.GetMimeMapping(filename), filename);
        }
    }
}
