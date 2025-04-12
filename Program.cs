using auctionbay_backend.Data;
using auctionbay_backend.Models;
using auctionbay_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Configure DB Context with MySQL.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

//configure ASP.NET Core Identity.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    //customize options if needed (password requirements).
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

//configure JWT from settings.
var jwtSettingsSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

//configure JWT authentication.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

//register custom authentication service
builder.Services.AddScoped<IAuthService, AuthService>();

//add controllers and endpoints explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuctionBay API",
        Version = "v1",
        Description = "API endpoints for the AuctionBay application."
    });

    //define the JWT Bearer authentication scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
         {
             new OpenApiSecurityScheme
             {
                 Reference = new OpenApiReference
                 {
                     Type = ReferenceType.SecurityScheme,
                     Id = "Bearer"
                 },
                 Scheme = "oauth2",
                 Name = "Bearer",
                 In = ParameterLocation.Header,
             },
             new List<string>()
         }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuctionBay API V1");
    });
}

//enable serving static files (e.g., images uploaded by users)
app.UseStaticFiles();

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

//vall the seeding method after building but before running
await SeedDatabaseAsync(app);

app.Run();

#region SEEDING
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    //Only seed if user1 doesn't exist (to avoid duplicate seeding).
    var checkUser = await userManager.FindByEmailAsync("user1@testing.com");
    if (checkUser != null)
    {
        return;
    }

    
    string sourceFolder = @"C:\Users\Matic\Desktop\auctionbay\imagesfordb";
    
    var imageFiles = new[]
    {
        "iphone14.jpg",
        "iphone15.jpg",
        "macbook.jpg"
    };

   
    foreach (var file in imageFiles)
    {
        var path = Path.Combine(sourceFolder, file);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Image not found: {path}");
        }
    }

    
    var firstNames = new[] { "Marjan", "Gozdni", "Jo�a", "Sramak", "Bogi", "James", "Kate", "Daniel", "Patricia", "Michael" };
    var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Wilson", "Anderson", "Taylor" };
    var randomTitles = new[]
    {
        "Cool Item",
        "Rare Collectible",
        "Vintage Treasure",
        "Gadget on Sale",
        "Exclusive Deal"
    };

    var rand = new Random();

    //Create 10 users and for each create 3 auctions.
    for (int i = 1; i <= 10; i++)
    {
        var fname = firstNames[rand.Next(firstNames.Length)];
        var lname = lastNames[rand.Next(lastNames.Length)];
        var email = $"user{i}@testing.com";

        var user = new ApplicationUser
        {
            FirstName = fname,
            LastName = lname,
            Email = email,
            UserName = email,
           
            
            ProfilePictureUrl = $"/imagesfordb/{imageFiles[rand.Next(imageFiles.Length)]}"
        };

        //Create user with a default password.
        var createResult = await userManager.CreateAsync(user, "Geslo123!");
        if (!createResult.Succeeded)
        {
            continue;
        }

        //For each user, create 3 auctions.
        for (int j = 1; j <= 3; j++)
        {
            var randomTitle = randomTitles[rand.Next(randomTitles.Length)] + $" #{j}";
            var randomImage = imageFiles[rand.Next(imageFiles.Length)];
            var imagePath = Path.Combine(sourceFolder, randomImage);

            byte[] imageData = File.Exists(imagePath) ? await File.ReadAllBytesAsync(imagePath) : null;

            var auction = new Auction
            {
                Title = randomTitle,
                Description = $"This is a description for {randomTitle}, created by {fname} {lname}.",
                StartingPrice = rand.Next(5, 300), // random price between 5 and 300
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddDays(rand.Next(3, 10)), // 3-10 days from now
                AuctionState = "Active",
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                //Save the file path as well if needed.
                MainImageUrl = $"/imagesfordb/{randomImage}",
                //Save binary image data.
                MainImageData = imageData
            };

            dbContext.Auctions.Add(auction);
        }
    }

    await dbContext.SaveChangesAsync();
    Console.WriteLine("Seeding completed: 10 users and 30 auctions created.");
}
#endregion
