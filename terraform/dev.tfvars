environment            = "dev"
sld_domain             = "bekk.dev"
hostname               = "api"
task_image             = "882089634282.dkr.ecr.eu-central-1.amazonaws.com/bekk-arrangement-svc"
task_image_tag         = "latest"
listener_path_patterns = ["/arrangement-svc*"]
task_environment = {
  ASPNETCORE_ENVIRONMENT       = "Development"
  Auth0__Issuer_Domain         = "bekk-dev.eu.auth0.com"
  Auth0__Audience              = "QHQy75S7tmnhDdBGYSnszzlhMPul0fAE"
  PORT                         = "80"
  Serilog__MinimumLevel        = "Warning"
  VIRTUAL_PATH                 = "/arrangement-svc"
  Auth0__PERMISSION_CLAIM_TYPE = "https://api.bekk.no/claims/permission"
}
task_secrets = [
  "ConnectionStrings__EventDb",
  "Sendgrid__Apikey"
]

