﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Eating.ViewModels
{
    public class SeatViewModel
    {
        public int Id { get; set; }

        [StringLength(10)]
        public string R_Id { get; set; }

        [Required, Display(Name ="桌號")]
        public string SeatName { get; set; }

        [Required, Display(Name ="座位數量")]
        public int SeatCapacity { get; set; }

        [Display(Name = "備註")]
        [StringLength(100)]
        public string SeatDetail { get; set; }

        [Required, Display(Name = "吸菸區")]
        public bool SeatSmoke { get; set; }

        public float location_X { get; set; }

        public float location_Y { get; set; }



    }
}