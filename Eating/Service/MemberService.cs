﻿using Eating.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using Eating.Repository;
using Eating.Interface;
using Eating.ViewModels;
using Eating.Areas.Backend.Models;
using AutoMapper;
using System.Linq.Expressions;
using Eating.DTOs;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Web.Hosting;

namespace Eating.Service
{
    public class MemberService
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private IRepository<Restaurant> repository = new GenericRepository<Restaurant>(new ApplicationDbContext());
        private IRepository<Menu> menuRep = new GenericRepository<Menu>(new ApplicationDbContext());
        /// <summary>
        /// Save restaurant new user info to database
        /// </summary>
        /// <param name="restaurant"></param>
        public void Register(Restaurant restaurant)
        {
            restaurant.R_Password  = HashPassword(restaurant.R_Password);
            db.Restaurants.Add(restaurant);
            db.SaveChanges();
        }
        
        public bool IdIsExist(string id)
        {
            string Url = $"http://data.gcis.nat.gov.tw/od/data/api/5F64D864-61CB-4D0D-8AD9-492047CC1EA6?$format=xml&$filter=Business_Accounting_NO eq {id}";
            var request = WebRequest.Create(Url) as HttpWebRequest;
            var response = request.GetResponse();

            Stream receiveStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);

            var result = readStream.ReadToEnd();
            var xml = XElement.Parse(result);
            if (xml.Elements("row").FirstOrDefault() == null)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        /// <summary>
        /// 加密Password
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public string HashPassword(string password)
        {
            string saltKey = "SDajg83Q2hrgsd9GFjdeSflm3gh5H";
            string saltAndPassword = string.Concat(password, saltKey);
            SHA1CryptoServiceProvider sha1Hasher = new SHA1CryptoServiceProvider();
            byte[] PasswordData = Encoding.Default.GetBytes(saltAndPassword);
            byte[] HashData = sha1Hasher.ComputeHash(PasswordData);
            string HashResult = "";

            for(int i = 0; i<HashData.Length; i++)
            {
                HashResult += HashData[i].ToString("x2");
            }

            return HashResult;
        }

        /// <summary>
        /// Check if the account exist
        /// </summary>
        /// <param name="Account"></param>
        /// <returns></returns>
        public bool AccountCheck(string Account)
        {
            Restaurant query = db.Restaurants.SingleOrDefault(a=>a.R_Account == Account);

            bool result = (query == null);
            return result;


        }


        public bool isCheck(string Account)
        {
            var q = db.Restaurants.Where(r => r.R_Account == Account).FirstOrDefault();
            return (q.StatusFlg == false ? false : true);
        }
        /// <summary>
        /// Validate Email
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="authCode"></param>
        /// <returns></returns>
        public string EmailValidate(string userName, string authCode)
        {
            Restaurant validateRestaurant = db.Restaurants.Find(userName);
            string ValidateStr = string.Empty;
            
            if(validateRestaurant != null)
            {
                if(validateRestaurant.AuthCode == authCode)
                {
                    validateRestaurant.AuthCode = string.Empty;
                    db.SaveChanges();
                    ValidateStr = "信箱驗證成功";
                }
                else
                {
                    ValidateStr = "驗證碼錯誤，請重新確認或再註冊";
                }
            }
            else
            {
                ValidateStr = "資料錯誤，請重新確認或再註冊";
            }

            return ValidateStr;
        }

        public string LoginCheck(string UserName, string Password)
        {
            Restaurant r = db.Restaurants.Where(u => u.R_Account == UserName).SingleOrDefault();

            if(r != null)
            {
                //when member valid his email address
                if (string.IsNullOrWhiteSpace(r.AuthCode))
                {
                    if(PasswordCheck(r, Password))
                    {
                        return "";
                        
                    }
                    {
                        return "密碼有誤！ 請重新確認";
                    }
                }
                else
                {
                    return "信箱尚未驗證成功，請先通過驗證後再次操作。";
                }
            }
            else
            {
                return "查無此帳號，請確認或是重新註冊";
            }
        }

        public bool PasswordCheck(Restaurant _restaurant, string _password)
        {
            bool result = _restaurant.R_Password.Equals(HashPassword(_password));

            return result;
        }

        public bool CheckId(string id)
        {
            return repository.Get(x => x.Id == id) != null ? true : false;  
        }
        public Restaurant GetRestaurant(string id)
        {
            return repository.Get(r => r.Id == id);
        }

        public RestaurantDetailRankingDTO GetRestaurantAndFeedback(string id)
        {
            var r_query = repository.Get(r => r.Id == id);
            var r_feedback = db.Feedbacks.Where(r => r.R_Id == id).ToList();
            var restaurantDetailRank = Mapper.Map<Restaurant, RestaurantDetailRankingDTO>(r_query);
            return restaurantDetailRank;
        }
        /// <summary>
        /// Get check restaurants
        /// </summary>
        /// <returns></returns>
        /// 
        public IEnumerable<RestaurantStatusVM> GetAllCheckList(Expression<Func<Restaurant, bool>> predicate)
        {
            var q = db.Restaurants.Where(predicate).Select(Mapper.Map<Restaurant, RestaurantStatusVM>).ToList();
            return q;
        }
        public IEnumerable<RestaurantStatusVM> GetAllCheckList()
        {
            var q = db.Restaurants.Select(Mapper.Map<Restaurant, RestaurantStatusVM>).ToList();
            return q;
        }



        public bool CheckOldPassword(string r_account, string checkPassword)
        {
            var password = repository.Get(r => r.R_Account == r_account).R_Password;
            return (password == checkPassword);
        }

        public IResult Update(Restaurant instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException();
            }
            IResult result = new Result(false);
            try
            {
                repository.Update(instance);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }
            return result;
        }

        public IEnumerable<RestaurantDetailDTO> GetAllList()
        {
            var query = repository.GetList().Where(f => f.StatusFlg == true).Select(Mapper.Map<Restaurant, RestaurantDetailDTO>).ToList();
            return query;
        }

        public IEnumerable<MenuListViewModel> GetMenuList(string r_id)
        {
            var lists = menuRep.GetList(r => r.R_Id == r_id).Select(Mapper.Map<Models.Menu, MenuListViewModel>).ToList();
            return lists;
        }

        public bool SaveMenu(int id, string name)
        {
            try
            {
                var query = menuRep.Get(i => i.Id == id);
                query.FoodName = name;
                menuRep.Update(query);
                return true;
            }
            catch(Exception e)
            {
                throw e;  
            }
            
        }

        public bool DelMenu(int id, string R_Id)
        {
            try
            {
                var query = menuRep.Get(i => i.Id == id);
                string tempPath = "~/Upload/Temp/";
                string serverMapPath = $"~/Upload/Menu/{R_Id}";
                string UrlBase = $"/Upload/Menu/{R_Id}";
                string StorageRoot = Path.Combine(HostingEnvironment.MapPath(serverMapPath));
                FilesHelper filesHelper = new FilesHelper(StorageRoot, UrlBase, tempPath, serverMapPath);
                string imgPath = query.ImgPath;
                string filename = imgPath.Substring(imgPath.LastIndexOf('/')+1);
                filesHelper.DeleteFile(filename);
                
                menuRep.Delete(query);

                return true;
            }
            catch(Exception e)
            {
                throw e;
            }
        }

    }
}