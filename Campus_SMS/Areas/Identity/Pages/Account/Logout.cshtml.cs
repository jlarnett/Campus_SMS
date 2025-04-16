// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Campus_SMS.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;

namespace Campus_SMS.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<AppUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public IActionResult OnPost(string returnUrl = null)
        {
            var domain = "dev-7aktrm4prqivmcb1.us.auth0.com";
            var clientId = "Yh6n7TYtyRiF8BcqugDt2K4K5LFR4SCQ";
            var returnTo = Url.Page("/Index", null, null, Request.Scheme);

            var logoutUrl = $"https://{domain}/v2/logout?client_id={clientId}&returnTo={Uri.EscapeDataString(returnTo)}";

            return SignOut(new AuthenticationProperties { RedirectUri = logoutUrl },
                IdentityConstants.ApplicationScheme,
                "Auth0");
        }
    }
}
