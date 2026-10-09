using Expresso.Parsing;
using Expresso.Sample.WebApi.EfCore.DataAccess;
using Expresso.Sample.WebApi.EfCore.Filtering;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRequestParametersParsers();
var fieldsProvider = new RequestFieldsInfoProvider();
SamplePolicySetup.Register<Expresso.Sample.WebApi.EfCore.Controllers.BooksController>(builder.Services, builder.Configuration, "book", fieldsProvider, Console.WriteLine);
SamplePolicySetup.Register<Expresso.Sample.WebApi.EfCore.Controllers.AuthorsController>(builder.Services, builder.Configuration, "author", fieldsProvider, Console.WriteLine);
SamplePolicySetup.Register<Expresso.Sample.WebApi.EfCore.Controllers.PublishersController>(builder.Services, builder.Configuration, "publisher", fieldsProvider, Console.WriteLine);
SampleEngineSetup.AddSampleEngine(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();
