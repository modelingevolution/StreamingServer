New-Cake -Name "Identity.Deployer" -Root "../Source" | Out-Null
Add-CakeCommonStep Pack { Build-Nuget -SpecFile "octopus-deploy.nuspec" }
Add-CakeCommonStep Publish { Publish-Nuget -SourceUrl "https://nuget.modelingevolution.com"}