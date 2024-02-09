 

using CI_Platform.Models;
using CI_Platform.Models.ViewModels;
using CI_Platform_Entites.Data;
using CI_Platform_Entites.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CI_Platform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly CiPlatformContext _db;

        private List<Mission> Missions = new List<Mission>();

        private List<MissionVM> missionVMList = new List<MissionVM>();

        private List<SavedStoriesVM> SavedStoriesVMList = new List<SavedStoriesVM>();

        public HomeController(ILogger<HomeController> logger, CiPlatformContext db)
        {
            _logger = logger;
            _db = db;
        }

  
        public IActionResult Index()
        {
             return View();
        }

        [HttpPost]
        public IActionResult Index(LoginVM model)

        {
            var Cd = _db.Admins.FirstOrDefault(a => a.Email == model.Email && a.Password == model.Password);
            if(Cd == null)
            {
                var Ab = _db.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);
                if (Ab == null)
                {
                    ViewBag.loginerror = "Email address & password does not match";
                    return View();
                }


                HttpContext.Session.SetString("UserId", Ab.UserId.ToString());
                return RedirectToAction("LandingPage", "Home", new { @id = Ab.UserId });
            }
 
            else
            {
                HttpContext.Session.SetString("AdminId", Cd.AdminId.ToString());
                return RedirectToAction("Admin", "Home", new { @id = Cd.AdminId });
            }

        }



        [HttpPost]
        public async Task<IActionResult> FilterMissions(string? SearchInput, long[] CountryFilter, long[] CityFilter, long[] MissionThemeFilter, long[] MissionSkillFilter, string MissionSort = "", int pg = 1)
        {
            const int pageSize = 9;
            Missions = _db.Missions.Include(m => m.MissionSkills).Where(x => x.DeletedAt == null).ToList();

            long UserId = Convert.ToInt64(HttpContext.Session.GetString("UserId"));
            var user = _db.Users.FirstOrDefault(e => e.UserId == UserId);
            ViewBag.user = user;

            var filter = new FilterVM
            {
                SearchInput = SearchInput?.ToLower(),
                CountryFilter = CountryFilter,
                CityFilter = CityFilter,
                ThemesFilter = MissionThemeFilter,
                SkillsFilter = MissionSkillFilter,

            };

            




        var result = from Cmission in _db.Missions
                         join Mission_Theme in _db.MissionThemes on Cmission.ThemeId equals Mission_Theme.MissionThemeId
                         select new { Mission_Theme.Title, Cmission.MissionId };
            var MyCity = from Mission in _db.Missions
                         join City in _db.Cities on Mission.CityId equals City.CityId
                         select new { Mission.CityId, City.Name };
            var Goal = from Gmission in _db.Missions
                       join GoalMission in _db.GoalMissions on Gmission.MissionId equals GoalMission.MissionId
                       select new { Gmission.MissionId, GoalMission.GoalValue, GoalMission.GoalObjectiveText };
            Missions = _db.Missions.Include(m => m.MissionSkills).Where(x => x.DeletedAt == null).ToList();
            ViewBag.Countries = _db.Countries.ToList();
            ViewBag.Cities = _db.Cities.ToList();
            ViewBag.Themes = _db.MissionThemes.ToList();
            ViewBag.Skills = _db.Skills.ToList();

            var Skills = _db.Skills.Where(m => m.DeletedAt == null);


            foreach (var mission in Missions)
            {
                var theme = result.Where(result => result.MissionId == mission.MissionId).FirstOrDefault();
                var city = MyCity.Where(MyCity => MyCity.CityId == mission.CityId).FirstOrDefault();
                var goal = Goal.Where(Goal => Goal.MissionId == mission.MissionId).FirstOrDefault();
                string[] startDate = mission.StartDate.ToString().Split(' ');
                string[] endDate = mission.EndDate.ToString().Split(' ');
                GoalMission? GOAL = _db.GoalMissions.FirstOrDefault(gm => gm.MissionId == mission.MissionId);

                missionVMList.Add(new MissionVM()
                {
                    MissionId = mission.MissionId,
                    Title = mission.Title,
                    ShortDescription = mission.ShortDescription,
                    Description = mission.Description,
                    Organization = mission.OrganizationName,
                    OrganizationDetails = mission.OrganizationDetail,
                    //Rating = mission.MissionRatings,
                    //ADD MISSION IMAGE URL HERE
                    //Theme = mission.Theme,
                    //ADD PROGRESS HERE
                    //ADD recent volunteers here
                    missionType = mission.MissionType,
                    isFavrouite = mission.FavoriteMissions.Any(),
                    createdAt = DateTime.Now,
                    Theme = theme.Title,
                    CityId = mission.CityId,
                    CountryId = mission.CountryId,
                    ThemeId = mission.ThemeId,


                    StartDate = startDate[0],
                    EndDate = endDate[0],
                    City = city.Name,
                    NoOfSeatsLeft = int.Parse(mission.Availability),
                    progress = GOAL != null ? int.Parse(GOAL.GoalValue) : 0,
                    GoalAim = GOAL != null ? GOAL.GoalObjectiveText : null,

                    MissionSkills = mission.MissionSkills.Join(Skills, ms => ms.MissionSkillId, s => s.SkillId, (ms, s) => ms).ToList(),


                });
            }

            // filtering
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.SearchInput))
                {
                    missionVMList = missionVMList.Where(m => m.Title.ToLower().Contains(filter.SearchInput)).ToList();
                }
                if (filter.CountryFilter != null && filter.CountryFilter.Length > 0)
                {
                     
                    missionVMList = missionVMList.Where(m => filter.CountryFilter.Any(x => x == m.CountryId)).ToList();
                    
                }
                if (filter.CityFilter != null && filter.CityFilter.Length > 0)
                {
                    missionVMList = missionVMList.Where(m => filter.CityFilter.Any(cf => cf == m.CityId)).ToList();
                }
                if (filter.ThemesFilter != null && filter.ThemesFilter.Length > 0)
                {
                    missionVMList = missionVMList.Where(m => filter.ThemesFilter.Any(tf => tf == m.ThemeId)).ToList();
                }
                if (filter.SkillsFilter != null && filter.SkillsFilter.Length > 0)
                {
                    missionVMList = missionVMList.Where(m => m.MissionSkills.Any(ms => filter.SkillsFilter.Any(sf => sf == ms.SkillId))).ToList();
                }
            }

            int missionCounts = missionVMList.Count();
            if (pg < 1)
                pg = 1;
            int totalPages = Pager.getTotalPages(missionCounts, pageSize);
            if (pg > totalPages)
                pg = totalPages;
            int recSkip = (pg - 1) * pageSize;
            var pager = new Pager(missionCounts, pg, pageSize);
            // Missions on Current page
            missionVMList = missionVMList.Skip(recSkip).Take(pager.PageSize).ToList();

            MissionListingVM missionListingVM = new MissionListingVM();
            missionListingVM.Missions = missionVMList;
            missionListingVM.MissionCount = missionCounts;
            missionListingVM.Pager = pager;

            ViewBag.missionListingVM = missionListingVM;


            return PartialView("_MissionListingPartial", missionListingVM);
        }

        //public async Task<IActionResult> FilterMissions(string SearchInput, string[] CountryFilter, string[] CityFilter, string[] MissionThemeFilter, string[] MissionSkillFilter, string MissionSort = "", int pg = 1)
        //{
        //    string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    const int pageSize = 3;
        //    var filter = new MissionFilter
        //    {
        //        SearchInput = SearchInput,
        //        Country = CountryFilter,
        //        City = CityFilter,
        //        MissionThemes = MissionThemeFilter,
        //        MissionSkills = MissionSkillFilter
        //    };
        //    List<MissionsDetailsVM> MissionsDetails = _unitOfWork.Mission.GetAllMissions(userId, filter, MissionSort);
    //    // Pagination Code
    //    int missionCounts = MissionsDetails.Count();
    //        if (pg< 1)
    //            pg = 1;
    //        int totalPages = Pager.getTotalPages(missionCounts, pageSize);
    //        if (pg > totalPages)
    //            pg = totalPages;
    //        int recSkip = (pg - 1) * pageSize;
    //    var pager = new Pager(missionCounts, pg, pageSize);
    //    // Missions on Current page
    //    List<MissionsDetailsVM> PageMissionsDetails = MissionsDetails.Skip(recSkip).Take(pager.PageSize).ToList();

    //    MissionListingVM MissionListing = new MissionListingVM
    //    {
    //        MissionCount = missionCounts,
    //        Pager = pager,
    //        Missions = PageMissionsDetails
    //    };

    //        return PartialView("Index", MissionListing);
    //}






    public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordVM model)
        {
            if (ModelState.IsValid)
            {
                var ABC = _db.Users.FirstOrDefault(u => u.Email == model.Email);
                if (ABC == null)
                {
                    ViewBag.Emailnotexist = "Email address does not exist";
                }
            }
            return View();
        }

        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registration(RegisterVM user)
        {
            if (ModelState.IsValid)
            {
                var obj = _db.Users.Where(e => e.Email == user.Email).FirstOrDefault();
                if (obj == null)
                {
                    // Hash the user's password using Bcrypt

                    //where(function(e){
                    //    return e.Email == user.Email
                    //})
                    var data = new User()
                    {
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Password = user.Password,
                        PhoneNumber = user.PhoneNumber,
                        CountryId = null,
                        CityId = null
                    };
                    _db.Users.Add(data);
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["userExists"] = "Email already exists";
                    return View(user);
                }


            }
            else
            {
                TempData["errorMessage"] = "Empty form can't be submitted";
                return View(user);
            }
        }

        public IActionResult ResetPassword()
        {
            return View();
        }



        public IActionResult LandingPage(long id,string sortOrder)
        {
            string myVariable = HttpContext.Session.GetString("UserId");
            if (myVariable == null)
            {
                return RedirectToAction("Index");
            }

            
            var user = _db.Users.FirstOrDefault(e => e.UserId == id);
            ViewBag.user = user;


            // Retrieve missions from the database
            var missions = _db.Missions.Include(m => m.MissionSkills).ToList();
            //missions = missions.Where( m => m.MissionType );



            var result = from Cmission in _db.Missions
                         join Mission_Theme in _db.MissionThemes on Cmission.ThemeId equals Mission_Theme.MissionThemeId
                         select new { Mission_Theme.Title, Cmission.MissionId };
            var MyCity = from Mission in _db.Missions
                         join City in _db.Cities on Mission.CityId equals City.CityId
                         select new { Mission.CityId, City.Name };
           
            //var Goal = from Gmission in _db.GoalMissions
            //           join GoalMission in _db.GoalMissions on Gmission.MissionId equals GoalMission.MissionId
            //           select new { Gmission.MissionId, GoalMission.GoalValue, GoalMission.GoalObjectiveText };
            Missions = _db.Missions.Include(m => m.MissionSkills).Where(x => x.DeletedAt == null).ToList();
             
            ViewBag.Countries = _db.Countries.ToList();
            ViewBag.Cities = _db.Cities.ToList();
            ViewBag.Themes = _db.MissionThemes.ToList();
            ViewBag.Skills = _db.Skills.ToList();

            var Skills = _db.Skills.Where(m => m.DeletedAt == null);
            List<GoalMission> goal = new List<GoalMission>();

            foreach (var mission in Missions)
            {
                var theme = result.Where(result => result.MissionId == mission.MissionId).FirstOrDefault();
                var city = MyCity.Where(MyCity => MyCity.CityId == mission.CityId).FirstOrDefault();
                //goal.Add(_db.GoalMissions.Where(Goal => Goal.MissionId == mission.MissionId).FirstOrDefault()!);
                string[] startDate = mission.StartDate.ToString().Split(' ');
                string[] endDate = mission.EndDate.ToString().Split(' ');

                GoalMission? GOAL = _db.GoalMissions.FirstOrDefault(gm => gm.MissionId == mission.MissionId);

                missionVMList.Add(new MissionVM()
                {
                    MissionId = mission.MissionId,
                    Title = mission.Title,
                    ShortDescription = mission.ShortDescription,
                    Description = mission.Description,
                    Organization = mission.OrganizationName,
                    OrganizationDetails = mission.OrganizationDetail,
                    //Rating = mission.MissionRatings,
                    //ADD MISSION IMAGE URL HERE
                    //Theme = mission.Theme,
                    //ADD PROGRESS HERE
                    //ADD recent volunteers here
                    missionType = mission.MissionType,
                    isFavrouite = mission.FavoriteMissions.Any(),
                    createdAt = DateTime.Now,
                    Theme = theme.Title,
                    MissionType = mission.MissionType,

                    StartDate = startDate[0],
                    EndDate = endDate[0],
                    //City = mission.City,
                    City = city.Name,
                    NoOfSeatsLeft = int.Parse(mission.Availability),
                    progress = GOAL != null ? int.Parse(GOAL.GoalValue) : 0,
                    GoalAim = GOAL != null ? GOAL.GoalObjectiveText : null,

                    MissionSkills = mission.MissionSkills.Join(Skills, ms => ms.MissionSkillId, s => s.SkillId, (ms, s) => ms).ToList(),


                });
            }
            // Sort missions based on sortOrder parameter
            switch (sortOrder)
            {
                case "highest":
                    missionVMList = missionVMList.OrderByDescending(m => m.NoOfSeatsLeft).ToList();
                    break;
                case "lowest":
                    missionVMList = missionVMList.OrderBy(m => m.NoOfSeatsLeft).ToList();
                    break;
                case "newest":
                    missionVMList = missionVMList.OrderByDescending(m => m.createdAt).ToList();
                    break;
                case "oldest":
                    missionVMList = missionVMList.OrderBy(m => m.createdAt).ToList();
                    break;
                case "deadline":
                    missionVMList = missionVMList.OrderBy(m => m.StartDate).ToList();
                    break;
                // Add more cases for other sorting options as needed
                default:
                    missionVMList = missionVMList.OrderBy(m => m.Title).ToList();
                    break;
            }

            int pg = 1;
            int pageSize = 9;
            int missionCounts = missionVMList.Count();
            if (pg < 1)
                pg = 1;
            int totalPages = Pager.getTotalPages(missionCounts, pageSize);
            if (pg > totalPages)
                pg = totalPages;
            int recSkip = (pg - 1) * pageSize;
            var pager = new Pager(missionCounts, pg, pageSize);
            // Missions on Current page
            missionVMList = missionVMList.Skip(recSkip).Take(pager.PageSize).ToList();


            MissionListingVM missionListingVM = new MissionListingVM
            {
                Missions = missionVMList,
                MissionCount = missionCounts,
                Pager = pager
            };


            return View(missionListingVM);
        }

        public IActionResult MissionDetailPage(long id, long missionId)
        {
            var user = _db.Users.FirstOrDefault(e => e.UserId == id);
            var Mission = _db.Missions.FirstOrDefault(m => m.MissionId == missionId);
            var GoalMission = _db.GoalMissions.FirstOrDefault(g => g.MissionId == missionId);
            //var city = _db.Cities.FirstOrDefault(c => c.CityId == Mission.CityId);
            var theme = _db.MissionThemes.FirstOrDefault(c => c.MissionThemeId == Mission.ThemeId);
            var StartDate = Mission.StartDate.ToString().Split(' ');
            var EndDate = Mission.EndDate.ToString().Split(' ');
            var skills = _db.MissionSkills.Include(s => s.Skill).ToList();


            ViewBag.user = user;
            ViewBag.Mission = Mission;
            ViewBag.GoalMission = GoalMission;
            //ViewBag.City = city;
            ViewBag.Theme = theme;
            ViewBag.StartDate = StartDate;
            ViewBag.EndDate = EndDate;
            ViewBag.Skills = skills;


            return View();
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }


        public IActionResult MissionListingPage()
        {
            return View();
        }


        public IActionResult StoryListingPage(long id)
        {
            //var user = _db.Users.FirstOrDefault(e => e.UserId == id);kk
            ViewBag.Countries = _db.Countries.ToList();
            ViewBag.Cities = _db.Cities.ToList();
            ViewBag.Themes = _db.MissionThemes.ToList();
            ViewBag.Skills = _db.Skills.ToList();

            //Story Listing
            var user = _db.Users.FirstOrDefault(e => e.UserId == id);

            var ApprovedStories = _db.Stories.Where(e =>  e.Status == "Approved" && e.DeletedAt == null).ToList();

            var result = from M in _db.Missions
                         join S in _db.Stories on M.MissionId equals S.MissionId
                         join MT in _db.MissionThemes on M.ThemeId equals MT.MissionThemeId
                         select new { MT.Title, M.MissionId, S };
            foreach (var ApprovedStory in ApprovedStories)
            {
                var theme = result.Where(e => e.MissionId == ApprovedStory.MissionId).FirstOrDefault();

                var author= _db.Users.FirstOrDefault(e => e.UserId == ApprovedStory.UserId);
                SavedStoriesVMList.Add(new SavedStoriesVM()
                {
                    StoryId = ApprovedStory.StoryId,
                    MissionTheme = theme.Title,
                    MissionId = ApprovedStory.MissionId,
                    UserId = ApprovedStory.UserId,
                    Title = ApprovedStory.Title,
                    Status = ApprovedStory.Status,
                    Description = ApprovedStory.Description,
                    PublishedAt = ApprovedStory.PublishedAt,
                    AuthorFirstName = author.FirstName,
                    AuthorLastName = author.LastName,
                });
            }

            //var Author = _db.Users.Where(u => u.UserId == SavedStoriesVMList[].UserId);
            //ViewBag.Author = Author;

            ViewBag.MyStories = ApprovedStories;

            ViewBag.Countries = _db.Countries.ToList();
            ViewBag.Cities = _db.Cities.ToList();
            ViewBag.Themes = _db.MissionThemes.ToList();
            ViewBag.Skills = _db.Skills.ToList();

            ViewBag.Result = result;
            ViewBag.Stories = _db.Stories.ToList();

            ViewBag.user = user;

             

            //ViewBag.missionListingVM = missionListingVM;
            return View(SavedStoriesVMList);

            //Story Listing Ends
            }

        public IActionResult ShareYourStoryPage(long id, ShareStoryVM StoryData,long StoryId)
        {
            if (StoryId == 0)
            {

                if (id == 0)
                {
                    id = StoryData.UserId;
                }


                var user = _db.Users.FirstOrDefault(e => e.UserId == id);
                ViewBag.Countries = _db.Countries.ToList();
                ViewBag.Cities = _db.Cities.ToList();
                ViewBag.Themes = _db.MissionThemes.ToList();
                ViewBag.Skills = _db.Skills.ToList();
                ViewBag.Missions = _db.Missions.ToList();

                ViewBag.user = user;

                return View();
            }
            else
            {
                var user = _db.Users.FirstOrDefault(e => e.UserId == id);
                ViewBag.Countries = _db.Countries.ToList();
                ViewBag.Cities = _db.Cities.ToList();
                ViewBag.Themes = _db.MissionThemes.ToList();
                ViewBag.Skills = _db.Skills.ToList();
                ViewBag.Missions = _db.Missions.ToList();

                ViewBag.user = user;

                var StoryDATA = _db.Stories.FirstOrDefault(d => d.StoryId == StoryId);
                ShareStoryVM StoryData1 = new ShareStoryVM
                {
                    StoryId=StoryId,
                    UserId = StoryDATA.UserId,
                    MissionId = StoryDATA.MissionId,
                    StoryTitle = StoryDATA.Title,
                    PublishedAt = StoryDATA.PublishedAt,
                    MyStory = StoryDATA.Description,
                    //URL = StoryDATA.URL,
                };
                 
                return View(StoryData1);
            }
        }

        [HttpPost]
        public IActionResult SaveStory(long id, ShareStoryVM StoryData)
        {

            var user = _db.Users.FirstOrDefault(e => e.UserId == StoryData.UserId);

            //if (ModelState.IsValid)
            //{
            Story story;
            if(StoryData.StoryId == 0) {
                story = new Story()
                {
                    MissionId = StoryData.MissionId,
                    UserId = StoryData.UserId,
                    Title = StoryData.StoryTitle,
                    Status = "Draft",
                    Description = StoryData.MyStory,
                    PublishedAt = StoryData.PublishedAt,

                };
            }
            else
            {
                story = _db.Stories.FirstOrDefault(e => e.StoryId == StoryData.StoryId);
                story.MissionId = StoryData.MissionId;
                story.UserId = StoryData.UserId;
                story.Title = StoryData.StoryTitle;
                story.Status = "Draft";
                story.Description = StoryData.MyStory;
                story.PublishedAt = StoryData.PublishedAt;
            }


                _db.Stories.Add(story);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Story Saved Successfully!";
                
                return RedirectToAction("ShareYourStoryPage","Home", new { id = StoryData.UserId });
            //}
            //else
            //{
            //    TempData["errorStoryMessage"] = "Empty form can't be submitted";
            //    return RedirectToAction("ShareYourStoryPage",StoryData);
            //}
 
        }

        [HttpPost]
        public IActionResult SubmitStory( ShareStoryVM StoryData)
        {
            var user = _db.Users.FirstOrDefault(e => e.UserId == StoryData.UserId);

            //if (ModelState.IsValid)
            //{
            Story story;
            if (StoryData.StoryId == 0)
            {
                story = new Story()
                {
                    MissionId = StoryData.MissionId,
                    UserId = StoryData.UserId,
                    Title = StoryData.StoryTitle,
                    Status = "Pending",
                    Description = StoryData.MyStory,
                    PublishedAt = StoryData.PublishedAt,
                    
            };
                _db.Stories.Add(story);
            }
            else
            {
                story = _db.Stories.FirstOrDefault(e => e.StoryId == StoryData.StoryId);
                story.MissionId = StoryData.MissionId;
                story.UserId = StoryData.UserId;
                story.Title = StoryData.StoryTitle;
                story.Status = "Pending";
                story.Description = StoryData.MyStory;
                story.PublishedAt = StoryData.PublishedAt;
                story.UpdatedAt = DateTime.Now;
             _db.Stories.Update(story);
            }


           
            _db.SaveChanges();

            return RedirectToAction("ShareYourStoryPage", "Home", new { id = StoryData.UserId } );
            //}
            //else
            //{
            //    TempData["errorStoryMessage"] = "Empty form can't be submitted";
            //    return RedirectToAction("ShareYourStoryPage",StoryData);
            //}
        }

        public IActionResult SavedStories(long id)
        {


            var user = _db.Users.FirstOrDefault(e => e.UserId == id);

            var MySavedStories = _db.Stories.Where(e => e.UserId == id && e.Status == "Draft").ToList();

            var result = from M in _db.Missions
                         join S in _db.Stories on M.MissionId equals S.MissionId
                         join MT in _db.MissionThemes on M.ThemeId equals MT.MissionThemeId
                         select new { MT.Title, M.MissionId, S.UserId };
            foreach (var SavedStory in MySavedStories)
            {
                var theme = result.Where(e => e.MissionId == SavedStory.MissionId).FirstOrDefault();


                SavedStoriesVMList.Add(new SavedStoriesVM()
                {
                    StoryId=SavedStory.StoryId,
                    MissionTheme = theme.Title,
                    MissionId = SavedStory.MissionId,
                    UserId = SavedStory.UserId,
                    Title = SavedStory.Title,
                    Status = SavedStory.Status,
                    Description = SavedStory.Description,
                    PublishedAt = SavedStory.PublishedAt, 
                }); 
            }

            ViewBag.MyStories = MySavedStories;

            ViewBag.Countries = _db.Countries.ToList();
            ViewBag.Cities = _db.Cities.ToList();
            ViewBag.Themes = _db.MissionThemes.ToList();
            ViewBag.Skills = _db.Skills.ToList();

            ViewBag.Result = result;
            ViewBag.Stories = _db.Stories.ToList();

            ViewBag.user = user;

            //ViewBag.missionListingVM = missionListingVM;
            return View(SavedStoriesVMList);
        }



        //        if (ModelState.IsValid)
        //            {
        //                var obj = _db.Users.Where(e => e.Email == user.Email).FirstOrDefault();
        //                if (obj == null)
        //                {
        //                    // Hash the user's password using Bcrypt

        //                    //where(function(e){
        //                    //    return e.Email == user.Email
        //                    //})
        //                    var data = new User()
        //                    {
        //                        FirstName = user.FirstName,
        //                        LastName = user.LastName,
        //                        Email = user.Email,
        //                        Password = user.Password,
        //                        PhoneNumber = user.PhoneNumber,
        //                        CountryId = null,
        //                        CityId = null
        //                    };
        //        _db.Users.Add(data);
        //                    _db.SaveChanges();
        //                    return RedirectToAction("Index");
        //    }
        //                else
        //                {
        //                    TempData["userExists"] = "Email already exists";
        //                    return View(user);
        //}


        //            }
        //            else
        //{
        //    TempData["errorMessage"] = "Empty form can't be submitted";
        //    return View(user);
        //}



        public IActionResult StoryDetailPage(long id,long StoryId)
        {
            
            var user = _db.Users.FirstOrDefault(e => e.UserId == id);
            ViewBag.Countries = _db.Countries.ToList();
            ViewBag.Cities = _db.Cities.ToList();
            ViewBag.Themes = _db.MissionThemes.ToList();
            ViewBag.Skills = _db.Skills.ToList();


            var Stories = from S in _db.Stories
                          join U in _db.Users on S.UserId equals U.UserId
                          join M in _db.Missions on S.MissionId equals M.MissionId
                          select new { S, U , M };

            var Story = Stories.FirstOrDefault(e => e.S.StoryId == StoryId);
            ViewBag.Story = Story;

            ViewBag.user = user;

            return View();
        }

        //public IActionResult MyProfilePage(long id)
        //{

        //    var user = _db.Users.FirstOrDefault(e => e.UserId == id);
        //    ViewBag.user = user;


        //    return View();
        //}

        public IActionResult TestView()
        {
            return View();
        }

        public IActionResult VolunteeringTimeSheet(long id)
        {
            var user = _db.Users.FirstOrDefault(e => e.UserId == id);
            ViewBag.user = user;
            return View();
        }

        //public IActionResult ModalPopUp(long id)
        //{
        //    var user = _db.Users.FirstOrDefault(e => e.UserId == id);
        //    ViewBag.user = user;
        //    return View();
        //}


        //Edit Profile
        public IActionResult MyProfilePage(long id)
        {
            //var userid = HttpContext.Session.GetString("userID");
            //UserVM user = new UserVM();
            var user = _db.Users.FirstOrDefault(e => e.UserId == id);
            ViewBag.user = user;

            var Cities = _db.Cities.ToList();
            ViewBag.Cities = Cities;

            var Countries = _db.Countries.ToList();
            ViewBag.Countries = Countries;

            var Skills = _db.Skills.ToList();
            ViewBag.Skills = Skills;
            //user.Singleuser = _CiPlatformContext.Users.FirstOrDefault(u => u.UserId == Convert.ToInt32(userid)); Pelethi commented
            //    var u = _db.Users.FirstOrDefault(u => u.UserId == Convert.ToInt32(userid));

            //    user.Countries = _db.Countries.ToList();
            //    user.cities = _db.Cities.ToList();
            //    user.userSkills = _db.UserSkills.Where(u => u.UserId == Convert.ToInt32(userid)).ToList();
            //    user.skills = _db.Skills.ToList();

            //    user.FirstName = u.FirstName;
            //    user.LastName = u.LastName;
            //    user.EmployeeId = u.EmployeeId;
            //    user.Title = u.Title;
            //    user.Department = u.Department;
            //    user.ProfileText = u.ProfileText;
            //    user.WhyIVolunteer = u.WhyIVolunteer;
            //    user.CountryId = u.CountryId;
            //    user.CityId = u.CityId;
            //    user.LinkedInUrl = u.LinkedInUrl;
            //    if (u.Avatar != null)
            //    {
            //        user.UserAvatar = u.Avatar;
            //    }



            //    var allskills = _db.Skills.ToList();
            //    ViewBag.allskills = allskills;
            //    var skills = from US in _db.UserSkills
            //                 join S in _db.Skills on US.SkillId equals S.SkillId
            //                 select new { US.SkillId, S.SkillName, US.UserId };

            //    var uskills = skills.Where(e => e.UserId == Convert.ToInt32(userid)).ToList();
            //    ViewBag.userskills = uskills;
            //    foreach (var skill in uskills)
            //    {
            //        var rskill = allskills.FirstOrDefault(e => e.SkillId == skill.SkillId);
            //        allskills.Remove(rskill);
            //    }
            //    ViewBag.remainingSkills = allskills;


            //    return View(user);
            //}

            //[HttpPost]
            //public async Task<IActionResult> EditProfileAsync(UserVM user)
            //{
            //    user.Countries = _db.Countries.ToList();
            //    user.cities = _db.Cities.ToList();
            //    var userid = HttpContext.Session.GetString("userID");
            //    var obj = _db.Users.Where(u => u.UserId == Convert.ToInt32(userid)).FirstOrDefault();

            //    obj.FirstName = user.FirstName;
            //    obj.LastName = user.LastName;
            //    obj.EmployeeId = user.EmployeeId;
            //    obj.Title = user.Title;
            //    obj.Department = user.Department;
            //    obj.ProfileText = user.ProfileText;
            //    obj.WhyIVolunteer = user.WhyIVolunteer;
            //    obj.LinkedInUrl = user.LinkedInUrl;
            //    obj.CityId = user.CityId;
            //    obj.CountryId = user.CountryId;

            //    if (user.Avatar != null)
            //    {
            //        var FileName = "";
            //        using (var ms = new MemoryStream())
            //        {
            //            await user.Avatar.CopyToAsync(ms);
            //            var imageBytes = ms.ToArray();
            //            var base64String = Convert.ToBase64String(imageBytes);
            //            FileName = "data:image/png;base64," + base64String;
            //        }
            //        obj.Avatar = FileName;
            //    }

            //    _db.Users.Add(obj);
            //    _db.Users.Update(obj);
            //    _db.SaveChanges();

            return View();
        }

        //Editprofile ChangePassword
        //[HttpPost]
        //public bool ChangePassword(string old, string newp, string cnf)
        //{

        //    var userid = HttpContext.Session.GetString("userID");
        //    var user = _db.Users.Where(e => e.UserId == Convert.ToInt32(userid)).FirstOrDefault();

        //    if (old != user.Password)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        var pass = _db.Users.FirstOrDefault(u => u.Password == old);
        //        pass.Password = newp;

        //        _db.Users.Update(pass);
        //        _db.SaveChanges();

        //        return true;
        //    }

        //}

        [HttpPost]
        public IActionResult MyProfilePage(MyProfilePageVM data)
        {
            if ( data != null )
            {
                string myVariable = HttpContext.Session.GetString("UserId");

                var user = _db.Users.FirstOrDefault(e => e.UserId == long.Parse(myVariable));
                ViewBag.user = user;

                //var id = _db.Cities.FirstOrDefault(c => c.Name == data.City);


                var UserEdit = _db.Users.FirstOrDefault(e => e.UserId == long.Parse(myVariable));

                UserEdit.FirstName = data.Name;
                UserEdit.LastName = data.Surname;
                UserEdit.EmployeeId = data.EmployeeID;
                //Manager = data.Manager,
                UserEdit.Title = data.Title;
                UserEdit.Department = data.Department;
                UserEdit.ProfileText = data.MyProfile;
                UserEdit.WhyIVolunteer = data.WhyIVolunteer;
                UserEdit.CityId = data.City;
                UserEdit.CountryId = data.Country;
                //Availability = data.Availability,
                //LinkedInUrl = data.LinkedIn,






                _db.Users.Update(UserEdit);
            _db.SaveChanges();
            return RedirectToAction("MyProfilePage");

        }
            else
            {
                string myVariable = HttpContext.Session.GetString("UserId");

                var user = _db.Users.FirstOrDefault(e => e.UserId == long.Parse(myVariable));
                ViewBag.user = user;
                TempData["errorMessage"] = "Empty form can't be submitted";
                return View(data);
            }

            return View();
        }

        public IActionResult Privacy(long id)
        {
            var user = _db.Users.FirstOrDefault(e => e.UserId == id);
            ViewBag.user = user;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        //[HttpPost]
        //public async Task<IActionResult> SaveUserSkills(long[] selectedSkills, long id)
        //{

        //    var abc = _db.UserSkills.Where(e => e.UserId == id).ToList();
        //    _db.RemoveRange(abc);
        //    _db.SaveChanges();
        //    foreach (var skills in selectedSkills)
        //    {
        //        //_UOW.missionRepo.AddUserSkills(skills, id);
        //    }

        //    return RedirectToAction("UserProfile", "Home", new RouteValueDictionary(new { id = id }));

        //}

        //public IActionResult ContactUs(MissionListingVM c_us)
        //{
        //    return View("MissionListingPage",c_us);
        //}


        [HttpPost]
        public IActionResult ContactUs(ContactUsVM c_us)
        {
            long UserId = Convert.ToInt64(HttpContext.Session.GetString("UserId"));

            if (ModelState.IsValid)
            {
                var data = new ContactUss()
                {

                    UserId = c_us.UserId,
                    FirstName = c_us.FirstName,
                    LastName = c_us.LastName,
                    Email = c_us.Email,
                    Subject = c_us.Subject,
                    Message = c_us.Message,
                    QcreatedAt = DateTime.Now,
                    
                };

                _db.ContactUsses.Add(data);
                _db.SaveChanges();
                return Ok("Form submitted!");

        }
        else
            {
                TempData["errorMessage"] = "Empty form can't be submitted";
                return BadRequest();
            }

}

        //var data = new User()
        //{
        //    FirstName = user.FirstName,
        //    LastName = user.LastName,
        //    Email = user.Email,
        //    Password = user.Password,
        //    PhoneNumber = user.PhoneNumber,
        //    CountryId = null,
        //    CityId = null
        //};
        //_db.Users.Add(data);
        //            _db.SaveChanges();
        //            return RedirectToAction("Index");



        public IActionResult AdminUser(long id, AdminUserAddVM model)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id);
            if(Admin == null)
            {
                Admin = _db.Admins.FirstOrDefault(e => e.AdminId == model.AdminId);
            }
            ViewBag.Admin = Admin;

            var UserData = _db.Users.Where(e => e.DeletedAt == null).ToList();
            ViewBag.UserData = UserData;

            var Countries = _db.Countries.ToList();
            ViewBag.Countries = Countries;


             
            var Cities = _db.Cities.ToList();
            ViewBag.Cities = Cities;


            DateTime currentDate = DateTime.Now;

                return View(model);
          

        }

        public IActionResult AdminUserEdit(long UserId, long AdminId)
        {
            var User = _db.Users.FirstOrDefault(e => e.UserId == UserId);

            AdminUserAddVM model = new AdminUserAddVM();

            model.UserId = User.UserId;
            model.Password = User.Password;
            model.FirstName = User.FirstName;
            model.LastName = User.LastName;
            model.Email = User.Email;
            model.EmployeeId = User.EmployeeId;
            model.Department = User.Department;
            model.CountryId = User.CountryId;
            model.CityId = User.CityId;
            model.ProfileText = User.ProfileText;
            model.Status = User.Status;
            model.AdminId = AdminId;
            model.Password = User.Password;

            var Countries = _db.Countries.Where(e => e.CountryId == User.CountryId).ToList();
            ViewBag.Countries = Countries;

            var Cities = _db.Cities.Where(e => e.CityId == User.CityId).ToList();
            ViewBag.Cities = Cities;

            //TempData["UserEditSuccess"] = "User Edited Successfully!";
            return RedirectToAction("AdminUser", model);


        }
         
        [HttpPost]
        public IActionResult DeleteUser(long id,long AdminId)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == AdminId);
            ViewBag.Admin = Admin;

            var User = _db.Users.FirstOrDefault(e => e.UserId == id);
            User.DeletedAt = DateTime.Now;
            _db.Update(User);
            _db.SaveChanges();

            return RedirectToAction("AdminUser", "Home", new { id = AdminId });
        }


        public IActionResult Admin(long id)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id );
            ViewBag.Admin = Admin;

            DateTime currentDate = DateTime.Now;

            var cms_data = _db.CmsPages.Where(e => e.DeletedAt == null).ToList();
            ViewBag.cms_data = cms_data;

            return View( );
 
        }

        [HttpPost]
        public IActionResult DeleteCMSPage(long id,long AdminId)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == AdminId);
            ViewBag.Admin = Admin;

            var CMS = _db.CmsPages.FirstOrDefault(e => e.CmsPageId == id);
            CMS.DeletedAt = DateTime.Now;
            _db.Update(CMS);
            _db.SaveChanges();

            TempData["DeleteSuccess"] = "Deleted CMS Page Successfully!";
            return RedirectToAction("Admin","Home",new { id = AdminId });
        }


        public IActionResult AdminUserAdd(AdminUserAddVM data,long id)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id && e.DeletedAt == null);

            AdminUserAddVM model = new AdminUserAddVM();

            DateTime currentDate = DateTime.Now;

            return View(model);
             
        }

        [HttpPost]
        public IActionResult AdminUserAddPost(AdminUserAddVM data)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == data.AdminId && e.DeletedAt == null);

            if(data.UserId == 0)
            {
                User model = new User();


                model.FirstName = data.FirstName;
                model.LastName = data.LastName;
                model.Email = data.Email;
                model.EmployeeId = data.EmployeeId;
                model.Department = data.Department;
                model.Status = data.Status;
                model.CityId = data.CityId;
                model.CountryId = data.CountryId;
                model.CreatedAt = DateTime.Now;
                model.Password = data.Password;

                _db.Users.Add(model);
                _db.SaveChanges();

                TempData["AddUserSuccess"] = "User Added Successfully!";
            }
            else
            {
                var model = _db.Users.Where(e => e.UserId == data.UserId).FirstOrDefault();

                model.FirstName = data.FirstName;
                model.LastName = data.LastName;
                model.Email = data.Email;
                model.EmployeeId = data.EmployeeId;
                model.Department = data.Department;
                model.Status = data.Status;
                model.CityId = data.CityId;
                model.CountryId = data.CountryId;
                model.CreatedAt = DateTime.Now;
                model.Password = data.Password;

                model.UpdatedAt = DateTime.Now;

                _db.Users.Update(model);
                _db.SaveChanges();

                TempData["AddUserSuccess"] = "User Updated Successfully!";

            }
            

            


            
            return RedirectToAction("AdminUser", "Home", new { id = data.AdminId });
        }



        public IActionResult CMSAdd(long id)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id && e.DeletedAt == null);
            //ViewBag.Admin = Admin;

             

            CMSAddVM model = new CMSAddVM();

            model.AdminId = Admin.AdminId;
            

            DateTime currentDate = DateTime.Now;
            return View(model);

        }

        [HttpPost]
        public IActionResult CMSAddPost(CMSAddVM model )
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == model.AdminId);
            //ViewBag.Admin = Admin;
 

            CmsPage data = new CmsPage()
            { 
                Title = model.Title,
                Description = model.Description,
                Slug = model.Slug,
                Status = model.Status,
                CreatedAt = DateTime.Now,
            };

            _db.CmsPages.Add(data);
            _db.SaveChanges();


            TempData["AddSuccess"]="CMS Page Added Successfully!";
            return RedirectToAction("Admin","Home",new { id = model.AdminId});
        }



        public IActionResult CMSEdit(long id,long CMSId )
        {
            var CMS = _db.CmsPages.FirstOrDefault(e=> e.CmsPageId == CMSId);
            //var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id);
            ////ViewBag.Admin = Admin;

            //var CMS = _db.CmsPages.FirstOrDefault(e => e.CmsPageId == CMSId && e.DeletedAt == null);
            ////ViewBag.CMS = CMS;

            CMSAddVM data = new CMSAddVM();
            data.Title = CMS.Title;
            data.Description = CMS.Description;
            data.Slug = CMS.Slug;
            data.Status = CMS.Status;
            data.AdminId = id;
            data.CMSId = CMSId;


            //data.Updated = datetime.now;
            return View( data);

        }

        [HttpPost]
        public IActionResult CMSEditPost(CMSAddVM model )
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == model.AdminId);
            ViewBag.Admin = Admin;

            var CMS = _db.CmsPages.FirstOrDefault(e => e.CmsPageId == model.CMSId && e.DeletedAt == null);
            ViewBag.CMS = CMS;
 

            var data = _db.CmsPages.FirstOrDefault(e => e.CmsPageId == model.CMSId && e.DeletedAt == null);
            data.Title = model.Title;
            data.Description= model.Description;
            data.Status= model.Status;
            data.Slug = model.Slug;
            
            data.UpdatedAt = DateTime.Now;

            _db.CmsPages.Update(data);
            _db.SaveChanges();



            TempData["EditSuccess"] = "CMS Page Edited Successfully!";
            return RedirectToAction("Admin", "Home", new { id = model.AdminId });
        }

        public IActionResult AdminMission(long id,AdminMissionAddVM model)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id);
            if (Admin == null)
            {
                Admin = _db.Admins.FirstOrDefault(e => e.AdminId == model.AdminId);
            }

            ViewBag.Admin = Admin;

            var AdminMission = _db.Missions.ToList();
            var ShowAdminMission = AdminMission.Where(x => x.DeletedAt == null);

            ViewBag.AdminMission = ShowAdminMission;

            var Countries = _db.Countries.ToList();
            ViewBag.Countries = Countries;

            var Cities = _db.Cities.ToList();
            ViewBag.Cities = Cities;

            var MissionThemes = _db.MissionThemes.ToList();
            ViewBag.MissionThemes = MissionThemes;

            var Skills = _db.Skills.ToList();
            ViewBag.Skills = Skills;


            DateTime currentDate = DateTime.Now;
            return View(model);

        }


        [HttpPost]
        public IActionResult AdminMissionAddPost(AdminMissionAddVM data)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == data.AdminId);
            ViewBag.Admin = Admin;

            if(data.MissionId == 0)
            {
                Mission mission = new Mission()
                {
                    CountryId = data.CountryId,
                    CityId = data.CityId,
                    ThemeId = data.ThemeId,
                    Title = data.Title,
                    Description = data.Description,
                    ShortDescription = data.ShortDescription,
                    StartDate = data.StartDate,
                    EndDate = data.EndDate,
                    MissionType = data.MissionType,
                    Status = data.Status,
                    OrganizationName = data.OrganizationName,
                    OrganizationDetail = data.OrganizationDetail,
                    Availability = data.Availability,
                    CreatedAt = DateTime.Now,

                };

                _db.Missions.Add(mission);
                _db.SaveChanges();


                TempData["AddMissionSuccess"] = "Mission Added Successfully!!!";

            }
            else
            {
                var model = _db.Missions.Where(e => e.MissionId == data.MissionId).FirstOrDefault();


                model.CountryId = data.CountryId;
                model.CityId = data.CityId;
                model.ThemeId = data.ThemeId;
                model.Title = data.Title;
                model.Description = data.Description;
                model.ShortDescription = data.ShortDescription;
                model.StartDate = data.StartDate;
                model.EndDate = data.EndDate;
                model.MissionType = data.MissionType;
                model.Status = data.Status;
                model.OrganizationName = data.OrganizationName;
                model.OrganizationDetail = data.OrganizationDetail;
                model.Availability = data.Availability;
                model.UpdatedAt = DateTime.Now;
 
                _db.Missions.Update(model);
                _db.SaveChanges();


                TempData["AddMissionSuccess"] = "Mission Updated Successfully!!!";

            }

           
            return RedirectToAction("AdminMission","Home",new { id = data.AdminId });
        }

        public IActionResult AdminMissionEdit(long MissionId, long AdminID)
        {
            var Mission = _db.Missions.Where(e => e.MissionId == MissionId).FirstOrDefault();

            AdminMissionAddVM model = new AdminMissionAddVM()
            {
                CountryId = Mission.CountryId,
                CityId = Mission.CityId,
                ThemeId = Mission.ThemeId,
                Title = Mission.Title,
                Description = Mission.Description,
                ShortDescription = Mission.ShortDescription,
                StartDate = Mission.StartDate,
                EndDate = Mission.EndDate,
                MissionType = Mission.MissionType,
                Status = Mission.Status,
                OrganizationName = Mission.OrganizationName,
                OrganizationDetail = Mission.OrganizationDetail,
                Availability = Mission.Availability,
                UpdatedAt = DateTime.Now,
                AdminId = AdminID,
                MissionId = MissionId,
            };





            var Countries = _db.Countries.Where(e => e.CountryId == Mission.CountryId).ToList();
            ViewBag.Countries = Countries;

            var Cities = _db.Cities.Where(e => e.CityId == Mission.CityId).ToList();
            ViewBag.Cities = Cities;

            //TempData["MissionEditSuccess"] = "Mission Edited Successfully!";
            return RedirectToAction("AdminMission", model );
 
        }

         


        public IActionResult AdminStory(long id)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id);
            ViewBag.Admin = Admin;

             
            var Stories = from S in _db.Stories
                          join U in _db.Users on S.UserId equals U.UserId
                          join M in _db.Missions on S.MissionId equals M.MissionId
                          select new { S, U.FirstName,U.LastName,M.Title, S.Status,U.UserId};

            var ShowStories = Stories.Where(x => x.Status == "Pending" && x.S.DeletedAt == null );  

            ViewBag.Stories = ShowStories;

            DateTime currentDate = DateTime.Now;
            return View();

        }

        [HttpPost]
        public IActionResult DeleteStory(long id,long AdminId)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == AdminId);
            ViewBag.Admin = Admin;

            var story = _db.Stories.FirstOrDefault(e => e.StoryId == id);
            story.DeletedAt = DateTime.Now;
            _db.Update(story);
            _db.SaveChanges();


            return RedirectToAction("AdminStory","Home", new { id = AdminId });
        }


        [HttpPost]
        public IActionResult DeleteMission(long id, long AdminId)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == AdminId);
            ViewBag.Admin = Admin;

            var mission = _db.Missions.FirstOrDefault(e => e.MissionId == id);
            mission.DeletedAt = DateTime.Now;
            _db.Update(mission);
            _db.SaveChanges();


            return RedirectToAction("AdminMission", "Home", new { id = AdminId });
        }

        [HttpPost]
        public IActionResult StoryUpdate(long id, long AdminId, string status)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == AdminId);
            ViewBag.Admin = Admin;


            var Stories = from S in _db.Stories
                          join U in _db.Users on S.UserId equals U.UserId
                          join M in _db.Missions on S.MissionId equals M.MissionId
                          select new { S, U.FirstName, U.LastName, M.Title, S.Status };

            var ShowStories = Stories.Where(x => x.Status == "Pending");

            ViewBag.Stories = ShowStories;

            DateTime currentDate = DateTime.Now;

            if (status == "Approved")
            {
                var data = _db.Stories.FirstOrDefault(e => e.StoryId == id);
                data.Status = "Approved";

                _db.Stories.Update(data);
                _db.SaveChanges();
            }
            else if (status == "Rejected")
            {
                var data = _db.Stories.FirstOrDefault(e => e.StoryId == id);
                data.Status = "Rejected";

                _db.Stories.Update(data);
                _db.SaveChanges();
            }



            return RedirectToAction("AdminStory", "Home", new { id = AdminId });

        }

        [HttpGet]
        public IActionResult AdminMissionApplication(long id)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id);
            ViewBag.Admin = Admin;

            var MissionApplications = from MA in _db.MissionApplications
                                      join U in _db.Users on MA.UserId equals U.UserId
                                      join M in _db.Missions on MA.MissionId equals M.MissionId
                                      select new { MA , M , U };

            var ShowMissionApplications = MissionApplications.Where(y => y.MA.ApprovalStatus == "Pending");

            ViewBag.MissionApplications = ShowMissionApplications;

             

            return View();
        }

        [HttpPost]
        public IActionResult AMissionApplication(long id, long AdminId, string status)
        {
            var Admin = _db.Admins.FirstOrDefault(e => e.AdminId == id);
            ViewBag.Admin = Admin;

            var MissionApplications = from MA in _db.MissionApplications
                                      join U in _db.Users on MA.UserId equals U.UserId
                                      join M in _db.Missions on MA.MissionId equals M.MissionId
                                      select new { MA, M, U };

            var ShowMissionApplications = MissionApplications.Where(y => y.MA.ApprovalStatus == "Pending");

            ViewBag.MissionApplications = ShowMissionApplications;

            if(status == "Approved")
            {
                var data = _db.MissionApplications.FirstOrDefault(e => e.MissionApplicationId == id);
                data.ApprovalStatus = "Approved";

                _db.MissionApplications.Update(data);
                _db.SaveChanges();
            }
            else if( status == "Rejected")
            {
                var data = _db.MissionApplications.FirstOrDefault(e => e.MissionApplicationId == id);
                data.ApprovalStatus = "Rejected";

                _db.MissionApplications.Update(data);
                _db.SaveChanges();
            }

           

            return RedirectToAction("AdminMissionApplication", "Home", new { id = AdminId });
        }



        public IActionResult MissionApplication(long id,long missionId)
        {
            var user = _db.Users.FirstOrDefault(e => e.UserId == id);
            ViewBag.user = user;

            var PreApplied = _db.MissionApplications.Where( PA => PA.UserId == id && PA.MissionId == missionId).FirstOrDefault();

            if(PreApplied == null)
            {
                var data = new MissionApplication()
                {
                    MissionId = missionId,
                    UserId = id,
                    AppliedAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    ApprovalStatus = "Pending",
                };


                _db.MissionApplications.Add(data);
                _db.SaveChanges();


                TempData["MissionSuccess"] = "Applied Successfully";

                return RedirectToAction("LandingPage", "Home", new { id = user.UserId });
            }
            else
            {
                TempData["AlreadyApplied"] = "You have already applied in this mission!";
                return RedirectToAction("MissionDetailPage","Home",new { id = user.UserId, missionId = missionId});
            }

           
        }













    }

    //         if (ModelState.IsValid)
    //            {
    //                var obj = _db.Users.Where(e => e.Email == user.Email).FirstOrDefault();
    //                if (obj == null)
    //                {
    //                    // Hash the user's password using Bcrypt

    //                    //where(function(e){
    //                    //    return e.Email == user.Email
    //                    //})
    //                    var data = new User()
    //                    {
    //                        FirstName = user.FirstName,
    //                        LastName = user.LastName,
    //                        Email = user.Email,
    //                        Password = user.Password,
    //                        PhoneNumber = user.PhoneNumber,
    //                        CountryId = null,
    //                        CityId = null
    //                    };
    //        _db.Users.Add(data);
    //                    _db.SaveChanges();
    //                    return RedirectToAction("Index");
    //    }
    //                else
    //                {
    //                    TempData["userExists"] = "Email already exists";
    //                    return View(user);
    //}


    //            }
    //            else
    //{
    //    TempData["errorMessage"] = "Empty form can't be submitted";
    //    return View(user);
    //}



}
