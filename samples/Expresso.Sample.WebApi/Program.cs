using Expresso.Core.Filtering;
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
builder.Services.AddSingleton<IRequestFieldsInfoProvider, RequestFieldsInfoProvider>();
SampleEngineSetup.AddSampleEngine(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
