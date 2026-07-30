using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace HajurKoCarRental.Areas.Identity.Data;

// Add profile data for application users by adding properties to the HajurKoCarRentalUser class
public class HajurKoCarRentalUser : IdentityUser
{
    public string? ProfilePhotoUrl { get; set; }
    [NotMapped]
    public IFormFile? ProfilePhoto { get; set; }


    public string? LicencePhotoUrl { get; set; }
    [NotMapped]
    public IFormFile? LicencePhoto { get; set; }

    public string? CitizenshipPhotoUrl { get; set; }
    [NotMapped]
    public IFormFile? CitizenshipPhoto { get; set; }

    public bool? RegularCustomer { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Address { get; set; }
    public bool? IsProfileVerified { get; set; } = false;
    public bool? IsProfileComplete { get; set; } = false;
    public bool? CanBook { get; set; } = true;
    public bool FrequentCustomerSeen { get; set; } = true;
}
