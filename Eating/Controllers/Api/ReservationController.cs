﻿
using Eating.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Eating.Controllers.Api
{
    public class ReservationController : ApiController
    {
        public IHttpActionResult GetReservationTime(DateTime date, int peopleNum, bool isSomke, string r_id)
        {
            

            return Ok();
        }
    }
}
