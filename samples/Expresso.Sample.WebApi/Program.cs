using Expresso.Sample.Shared.Filtering;
using Expresso.Parsing;
using Expresso.Sample.Shared.DataAccess;
using Expresso.Sample.WebApi.DataAccess;
using Expresso.Sample.WebApi.Filtering;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRequestParametersParsers();
var fieldsProvider = new RequestFieldsInfoProvider();
SamplePolicySetup.Register<Expresso.Sample.WebApi.Controllers.BooksController>(builder.Services, builder.Configuration, "book", fieldsProvider, Console.WriteLine);
SamplePolicySetup.Register<Expresso.Sample.WebApi.Controllers.AuthorsController>(builder.Services, builder.Configuration, "author", fieldsProvider, Console.WriteLine);
SamplePolicySetup.Register<Expresso.Sample.WebApi.Controllers.PublishersController>(builder.Services, builder.Configuration, "publisher", fieldsProvider, Console.WriteLine);
SampleEngineSetup.AddSampleEngine(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
