New-Cake -Name "TcpMultiplexer.Server" -Root "../Source" | Out-Null
Add-CakeCommonStep Build { Build-DockerImage -ImageName "tcpmultiplexer" -IsLatest -Dockerfile ./TcpMultiplexer.Server/Dockerfile -Repository "docker.modelingevolution.com"  }
Add-CakeCommonStep Deploy { Publish-DockerImage }