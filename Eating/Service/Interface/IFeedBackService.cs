﻿using Eating.Interface;
using Eating.Models;
using Eating.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eating.Service.Interface
{
    interface IFeedBackService
    {
        IResult Create(Feedback instance);

        IResult Update(Feedback instance);

        bool IsExists(int Id);

        Feedback GetByID(int Id);

        IEnumerable<Feedback> GetAll();

        IEnumerable<Feedback> GetFeedbackByRAccount(string r_Account);

        IEnumerable<FeedbacRatingkViewModel> GetRatingVM(string r_Accoun);
    }
}