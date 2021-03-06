﻿using Eating.Models;
using Eating.Service;
using Eating.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using AutoMapper;
using System.Web.UI.WebControls;
using Eating.Areas.Backend.Service;
using System.IO;
using System.Xml.Linq;
using System.Web.Hosting;

namespace Eating.Controllers
{
    public class RestaurantController : Controller
    {
        private MemberService memberService = new MemberService();
        private MailService mailService = new MailService();
        private AddressService addressService = new AddressService();
        private ApplicationDbContext db = new ApplicationDbContext();
       
      
        String DeleteURL = "/FileUpload/DeleteFile/?file=";
        String DeleteType = "GET";

        [AllowAnonymous]
        public ActionResult Register()
        {
            var model = new RestaurantRegisterViewModel
            {
                Counties = db.Counties
            };
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult RegisterResult()
        {
            return View();
        }

        /// <summary>
        /// Email Validate
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="AuthCode"></param>
        /// <returns></returns>
        public ActionResult EmailValidate(string UserName, string AuthCode)
        {
            ViewData["EmailValidate"] = memberService.EmailValidate(UserName, AuthCode);
            return View();
        }

        public ActionResult Menu()
        {
            return View();
        }

        [HttpGet]
        public JsonResult Areas(string county)
        {
            var q = db.Counties.Where(x => x.CountyName == county).Select(x => new { Id = x.Id });
            var list = addressService.GetAllAreas(q.First().Id);
            //產品
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ChangeWaitStatus(bool b)
        {

            string r_id = Request.Cookies["idCookie"].Values["r_id"];
            var q = memberService.GetRestaurant(r_id);
            q.WaitListSwitch = b;
            var result = memberService.Update(q);
            return result.Success ? new HttpStatusCodeResult(HttpStatusCode.OK) :
            new HttpStatusCodeResult(HttpStatusCode.BadRequest);


        }
       
        public RestaurantRegisterViewModel InitVM(RestaurantRegisterViewModel vm)
        {
            int _id = 0;
            if (vm.R_County != null)
            {
                _id = db.Counties.Where(x => x.CountyName == vm.R_County).SingleOrDefault().Id;
            }

            var registVM = new RestaurantRegisterViewModel
            {
                R_Password = null,
                checkR_Password = null,
                Counties = db.Counties,
                Areas = (_id == 0 ? null : addressService.GetAllAreas(_id))

            };
            return registVM;
        }


        public JsonResult AccountCheck(RestaurantRegisterViewModel registerMember)
        {
            return Json(memberService.AccountCheck(registerMember.R_Account),
                JsonRequestBehavior.AllowGet);
        }


      
        [CustomAuthorization(LoginPage = "/Restaurant/Login", Roles = "User")]
        public ActionResult Info()
        {
            var restaurant = memberService.GetRestaurant(Request.Cookies["idCookie"].Values["r_id"]);
            var infoVM = new RestaurantInfoViewModel();
            Mapper.Map(restaurant, infoVM);
            return View(infoVM);
        }

       
        /// <summary>
        /// 儲存修改後之餐廳一般資料
        /// </summary>
        /// <param name="_infoVM"></param>
        /// <returns></returns>
        public ActionResult SaveInfo(RestaurantInfoViewModel _infoVM)
        {
            if (!ModelState.IsValid)
            {
                return View("Info", _infoVM);
            }
       
            var R_Id = Request.Cookies["idCookie"].Values["r_id"];
            if (!Directory.Exists(Server.MapPath($"~/Upload/{R_Id})")))
            {
                DirectoryInfo di = Directory.CreateDirectory(Server.MapPath($"~/Upload/{R_Id}"));
            }
            var instance = memberService.GetRestaurant(R_Id);
            //if has new image to upload  2017/11/8 ChrisWang
            if (_infoVM.ImageFile != null)
            {
                string fileName = Path.GetFileNameWithoutExtension(_infoVM.ImageFile.FileName);
                string extension = Path.GetExtension(_infoVM.ImageFile.FileName);
                fileName = fileName + extension;
                _infoVM.ImagePath = string.Format($"/Upload/{R_Id}/{fileName}");
                fileName = Path.Combine(Server.MapPath($"/Upload/{R_Id}/"), fileName);
                _infoVM.ImageFile.SaveAs(fileName);
            }


            Mapper.Map(_infoVM, instance);
            var result = memberService.Update(instance);

            if (result.Success)
            {
                TempData["Messageinfo"] = "bootbox.alert('修改完成');";
                TempData["infoClassColor"] = "green";

            }
            else
            {
                TempData["Messageinfo"] = "bootbox.alert('Error！請重新確認後再試一次');";
                TempData["infoClassColor"] = "red";
            }
            return RedirectToAction("Info");

        }

        [HttpPost]
        public ActionResult SaveMenuName(int id, string name)
        {
            if(memberService.SaveMenu(id, name))
                return new HttpStatusCodeResult(HttpStatusCode.OK);            
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

        }

        [HttpPost]
        public ActionResult DeleteMenu(int id)
        {
            var R_Id = Request.Cookies["idCookie"].Values["r_id"];
            if (memberService.DelMenu(id, R_Id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
        [HttpPost]
        public JsonResult Upload(ICollection<string> listNames)
        {
            var R_Id = Request.Cookies["idCookie"].Values["r_id"];
            string tempPath = "~/Upload/Temp/";
            string serverMapPath = $"~/Upload/Menu/{R_Id}";
            string UrlBase = $"/Upload/Menu/{R_Id}";
            string StorageRoot = Path.Combine(HostingEnvironment.MapPath(serverMapPath));
            if (!Directory.Exists(Server.MapPath(serverMapPath)))
            {
                DirectoryInfo di = Directory.CreateDirectory(Server.MapPath(serverMapPath));
            }

            FilesHelper filesHelper = new FilesHelper(DeleteURL, DeleteType, StorageRoot, UrlBase, tempPath, serverMapPath, listNames);
            var resultList = new List<ViewDataUploadFilesResult>();

            var CurrentContext = HttpContext;

            filesHelper.UploadAndShowResults(CurrentContext, resultList);
            JsonFiles files = new JsonFiles(resultList);

            bool isEmpty = !resultList.Any();
            if (isEmpty)
            {
                return Json("Error ");
            }
            else
            {
                return Json(files);
            }
        }
        public JsonResult GetFileList()
        {
            
            var R_Id = Request.Cookies["idCookie"].Values["r_id"];
            string tempPath = "~/Upload/Temp/";
            string UrlBase = $"/Upload/Menu/{R_Id}";
            string serverMapPath = $"~/Upload/Menu/{R_Id}";       
            string StorageRoot = Path.Combine(HostingEnvironment.MapPath(serverMapPath));

            FilesHelper filesHelper = new FilesHelper(DeleteURL, DeleteType, StorageRoot, UrlBase, tempPath, serverMapPath);
            var list = filesHelper.GetFileList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        /// <summary>
        /// 儲存修改後餐廳帳號資料
        /// </summary>
        /// <param name="r_AccountVM"></param>
        /// <returns></returns>
        public ActionResult SaveAccount(RestaurantAccountViewModel r_AccountVM)
        {
            var _R_AccountVM = new RestaurantAccountViewModel()
            {
                R_Account = memberService.GetRestaurant(User.Identity.Name).R_Account
            };
            if (!ModelState.IsValid)
            {
                 return View("Info", _R_AccountVM);
            }

            var checkPassword = memberService.LoginCheck(User.Identity.Name, r_AccountVM.R_Password);
            if(!string.IsNullOrWhiteSpace(checkPassword))
            {
                ModelState.AddModelError("R_Password", "舊密碼有誤，請重新再試一次");
                return View("Info", _R_AccountVM);
            }

            var restaurant = memberService.GetRestaurant(User.Identity.Name);
            restaurant.R_Password = memberService.HashPassword(r_AccountVM.new_R_Password);
           var result =  memberService.Update(restaurant);
            if (result.Success)
            {
                TempData["Messageacc"] = "bootbox.alert('密碼已成功儲存，下次請以新密碼登入');";
                TempData["classColor"] = "green";

            }
            else
            {
                TempData["Messageacc"] = "bootbox.alert('儲存失敗');";
                TempData["classColor"] = "red";
            }
            return RedirectToAction("Info");
        }

        #region Login and Regist
        public ActionResult Login()
        {

            return View();
        }

        

        [HttpPost]
        public ActionResult Login(MemberLoginViewModel LoginVM, string returnUrl)
        {
            string ValidateStr = memberService.LoginCheck(LoginVM.R_Account, LoginVM.R_Password);

            //When ValidateStr is empty string, it'll login successful
            if (string.IsNullOrEmpty(ValidateStr))
            {
                if (!memberService.isCheck(LoginVM.R_Account))
                {
                    ModelState.AddModelError("", "尚未通過真實驗證，請等候通過");
                    return View();
                }
                var r = db.Restaurants.Where(a => a.R_Account == LoginVM.R_Account).SingleOrDefault();
                var r_id = r.Id;
                HttpContext.Session.Clear();
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1,
                    r.R_Name,
                    DateTime.Now,
                    DateTime.Now.AddHours(24),
                    false,
                    "User"
                    );

                var idCookie = new HttpCookie("idCookie");
                idCookie.Expires.AddHours(24);
                idCookie.Values.Add("r_id", r_id.ToString());
                //Encrypt cookie
                string enTicket = FormsAuthentication.Encrypt(ticket);
                Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, enTicket));
                Response.Cookies.Add(idCookie);

                string decodedUrl = "";
                if (!string.IsNullOrEmpty(returnUrl))
                    decodedUrl = Server.UrlDecode(returnUrl);

                //Login logic...

                if (Url.IsLocalUrl(decodedUrl))
                {
                    return Redirect(decodedUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }

            }
            else //Login failed
            {
                ModelState.AddModelError("", ValidateStr);
                return View(LoginVM);
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Register(RestaurantRegisterViewModel registerMember)
        {
            //var errors = ModelState.Values.SelectMany(v => v.Errors);

            if (ModelState.IsValid)
            {
                if (!memberService.IdIsExist(registerMember.Id))
                {
                    ModelState.AddModelError("Id", "無效之統一編號，請重新確認");
                    var registVM = InitVM(registerMember);
                    return View(registVM);
                }
                if (memberService.CheckId(registerMember.Id))
                {
                    var area = db.Counties.Where(x => x.CountyName == registerMember.R_County).Select(x => new { Id = x.Id });

                    ModelState.AddModelError("Id", "該統編已註冊過，請勿重複註冊");
                    var registVM = InitVM(registerMember);
                    return View(registVM);
                }
                var checkAccount = memberService.AccountCheck(registerMember.R_Account);
                if (checkAccount == false)
                {
                    var area = db.Counties.Where(x => x.CountyName == registerMember.R_County).Select(x => new { Id = x.Id });

                    ModelState.AddModelError("R_Account", "該帳號已被註冊，請重新嘗試");
                    var registVM = InitVM(registerMember);
                    return View(registVM);
                }

                string AuthCode = mailService.GetValidateCode(); //get random code
                double lat = 0.0, lng = 0.0;
                #region get lat and lon
                try
                {
                    var address = registerMember.R_County + registerMember.R_Area + registerMember.DetailAddr;
                    var requestUri = string.Format("http://maps.googleapis.com/maps/api/geocode/xml?address={0}", address);
                    var request = WebRequest.Create(requestUri);
                    var response = request.GetResponse();
                    var xdoc = XDocument.Load(response.GetResponseStream());
                    var result = xdoc.Element("GeocodeResponse").Element("result");
                    var locationElement = result.Element("geometry").Element("location");
                    lat = float.Parse(locationElement.Element("lat").Value);
                    lng = float.Parse(locationElement.Element("lng").Value);


                }
                catch (Exception e)
                {
                    ModelState.AddModelError("DetailAddr", "地址剖析失敗，請確認地址是否有效。");
                    var registVM = InitVM(registerMember);
                    return View(registVM);
                    throw e;
                }

                #endregion

                var localTime = DateTime.Now;

                TimeZoneInfo destTz =
                TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                var registTime = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, destTz);
                Restaurant _newMember = new Restaurant()
                {
                    Id = registerMember.Id,
                    R_Account = registerMember.R_Account,
                    R_Password = registerMember.R_Password,
                    R_PhoneNum = registerMember.R_PhoneNum,
                    R_Name = registerMember.R_Name,
                    Email = registerMember.Email,
                    SignUpTime = registTime,
                    R_County = registerMember.R_County,
                    R_Area = registerMember.R_Area,
                    R_DetailAddress = registerMember.DetailAddr,
                    AuthCode = AuthCode,
                    StartTime = registerMember.StartTime,
                    CloseTime = registerMember.CloseTime,
                    StatusFlg = false,
                    isCheck = false,
                    Lat = lat,
                    Lng = lng,
                    WaitListSwitch = false


                };

                string TempMail = System.IO.File.ReadAllText(
                    Server.MapPath("~/Views/Shared/RegisterEmailTemplate.html"));

                UriBuilder ValidateUrl = new UriBuilder(Request.Url)
                {
                    Path = Url.Action("EmailValidate", "Restaurant"
                    , new
                    {
                        UserName = registerMember.Id,
                        AuthCode = AuthCode
                    })
                };

                memberService.Register(_newMember);
                //Write user data to validate mail sample
                string MailBody = mailService.GetRegisterMailBody(TempMail,
                     registerMember.R_Name,
                     ValidateUrl.ToString().Replace("%3F", "?"));
                mailService.SendRegisterMail(MailBody, registerMember.Email);

                TempData["RegisterState"] = "註冊成功,請到個人信箱驗證Email地址";

                return View("RegisterResult");
            }
            else
            {
                var registVM = InitVM(registerMember);
                return View(registVM);
            }
        }


        [Authorize]
        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
        #endregion

        #region partal method
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        public ActionResult MenuList()
        {
            var R_id = Request.Cookies["idCookie"].Values["r_id"];
            var list = memberService.GetMenuList(R_id);
            return PartialView("_MenuList",list);
        }
        public ActionResult AccPartial()
        {
            var restaurant = memberService.GetRestaurant(Request.Cookies["idCookie"].Values["r_id"]);
            var accVM = new RestaurantAccountViewModel();
            Mapper.Map(restaurant, accVM);
            accVM.R_Password = string.Empty;
            return PartialView("_AccountPartial", accVM);
        }

        public ActionResult InfoPartial()
        {
            var account = memberService.GetRestaurant(Request.Cookies["idCookie"].Values["r_id"]);
            account.R_Password = null;
            var infoVM = new RestaurantInfoViewModel();
            Mapper.Map(account, infoVM);
            return PartialView("_InfoPartial", infoVM);
        }

        public ActionResult _MenuUpload()
        {

            return PartialView();
        }

    }
#endregion
}