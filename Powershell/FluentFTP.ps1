<#
 .Synopsis
  List the contents a FTP folder.

 .Description
  Connect to a FTP site and list the contents

 .Parameter Site
  The site to connect to.

 .Parameter User
  The user name.

 .Parameter Password
  Password associated with FTP site.

 .Parameter FtpDirectory
  The Directory on FTP server
  
  .Parameter FtpfileName
  Filename or wildcard to display
 .Example
   # List the files that start with text in the pub folder
   Show-FtpFile -Site ftp.site.com -User bob -Password secure -FtpDirectory pub -FtpFileName "text*"

#>
function Show-FtpFile {
param(
    [Parameter(Mandatory=$true)]
    [string] $site,
    [Parameter(Mandatory=$true)]
    [string] $user,
    [Parameter(Mandatory=$true)]
    [string] $password,
    [string] $ftpDirectory = "",
    [Parameter(Mandatory=$true)]
    [string] $ftpFileName
    )
   try
    {
        
        # Load FluentFTP .NET assembly
        Add-Type -Path "$PSScriptRoot\FluentFTP.dll"
        # Setup session options
        $client = New-Object FluentFTP.FtpClient($site)
        $client.Credentials = New-Object System.Net.NetworkCredential($user, $password)
        $client.AutoConnect()
		if ($ftpDirectory -ne "")
		{
			$client.SetWorkingDirectory($ftpDirectory)
		}          
		$currentDirectory = $client.GetWorkingDirectory()
        try
        {
            foreach ($item in $client.GetListing(""))
            {
				if ($item.Name -like $ftpFileName)
				{
					Write-Output "$item"
				}
            }
        }
        finally
        {
            # Disconnect, clean up
            $client.Disconnect()
        }
     
        
    }
    catch
    {
        Write-Output $_.Exception|format-list -force
       
    }
}


<#
 .Synopsis
  Rename a file in an FTP folder.

 .Description
  Connect to a FTP site and lis the contents

 .Parameter Site
  The site to connect to.

 .Parameter User
  The user name.

 .Parameter Password
  Password associated with FTP site.

 .Parameter DirectoryName
  The Directory on FTP server
  
  .Parameter oldName
  Old filename
  
    .Parameter NewName
  New filename
  
 .Example
   # Rename a file from old file name to new filename
   Rename-File -Site ftp.site.com -User bob -Password secure -FtpDirectory pub -oldName "Readme.txt -newName Readme.done"

#>
function Rename-FtpFile {
param(
    [Parameter(Mandatory=$true)]
    [string] $site,
    [Parameter(Mandatory=$true)]
    [string] $user,
    [Parameter(Mandatory=$true)]
    [string] $password,
    [string] $ftpDirectory = "",
    [Parameter(Mandatory=$true)]
    [string] $oldName,
    [Parameter(Mandatory=$true)]
    [string] $newName
    )

    try
    {
        
        # Load FluentFTP .NET assembly
        Add-Type -Path "$PSScriptRoot\FluentFTP.dll"
        # Setup session options
        $client = New-Object FluentFTP.FtpClient($site)
        $client.Credentials = New-Object System.Net.NetworkCredential($user, $password)
        $client.AutoConnect()

        try
        {
            if ($ftpDirectory -ne "")
            {
                $client.SetWorkingDirectory($ftpDirectory)
            }          
            $currentDirectory = $client.GetWorkingDirectory()
            if ($currentDirectory -match '/$')
            {
                $oldPath = "$currentDirectory$oldName"
                $newPath = "$currentDirectory$newName"
            }
            else
            {
                $oldPath = "$currentDirectory/$oldName"
                $newPath = "$currentDirectory/$newName"
            }
            if ($client.FileExists($oldPath))
            {
                $result = $client.Rename($oldPath, $newPath)
                if ($result)
                {
                    Write-Output "$oldPath successfully renamed to $newPath"
                }
            }
            else
            {
                Write-Output "$oldPath is not found on server"
            }
        }
        finally
        {
            # Disconnect, clean up
            $client.Disconnect()
        }    
    }
    catch [Exception]
    {
      echo $_.Exception|format-list -force
      
    }
}

<#
 .Synopsis
  Copy a file to an FTP folder.

 .Description
  Copy a file to an FTP folder.

 .Parameter Site
  The site to connect to.

 .Parameter User
  The user name.

 .Parameter Password
  Password associated with FTP site.

 .Parameter FtpDirectory
  The Directory on FTP server
  
  .Parameter fileName
  Filename to transfer
  
 .Example
   # Copy a file or group of fles to an FTP folder.
   Send-FtpFile -Site ftp.site.com -User bob -Password secure -FtpDirectory pub -fileName "Read*"

#>
function Send-FtpFile {
param(
    [Parameter(Mandatory=$true)]
    [string] $site,
    [Parameter(Mandatory=$true)]
    [string] $user,
    [Parameter(Mandatory=$true)]
    [string] $password,
    [string] $ftpdirectory = "",
    [Parameter(Mandatory=$true)]
    [string] $FtpFileName,
    [switch] $binary = $false
    )

    try
    {
        
        # Load FluentFTP .NET assembly
        Add-Type -Path "$PSScriptRoot\FluentFTP.dll"
        # Setup session options
        $client = New-Object FluentFTP.FtpClient($site)
        $client.Credentials = New-Object System.Net.NetworkCredential($user, $password)
        if ($binary)
        {
            $dataType = "Binary"
        }
        else
        {
            $dataType = "ASCII"
        }
            
        $client.UploadDatatype = $dataType
        $client.AutoConnect()
        if ($FtpFileName -notlike "*\*" )
        {
            $FtpFileName = Join-path $pwd $FtpFileName
        }
        $lclFile = Split-Path $FtpFileName -leaf
        $lclDir = Split-Path $FtpFileName -Parent
        try
        {
            if ($ftpdirectory -ne "")
            {
                $client.SetWorkingDirectory($ftpdirectory)
            }
            
            $currentDirectory = $client.GetWorkingDirectory()
            $wildFiles = [IO.Directory]::GetFiles($lclDir, $lclFile);
            $filesUploaded = $false
            foreach ($filePath in $wildFiles)
            {
                $fileOnly = Split-Path $filePath -leaf 
                if ($currentDirectory -match '/$')
                {
                    $ftpPath = "$currentDirectory$fileOnly"
                }
                else
                {
                    $ftpPath = "$currentDirectory/$fileOnly"
                }
                $result = $client.UploadFile($filePath, $ftpPath)
                $filesUploaded = $true
                Write-Output "$filePath successfully copied to $ftpPath"
            }
            if (!$filesUploaded)
            {
                Write-Output "No files matching $FtpFileName were found"
            }
        }
        finally
        {
            # Disconnect, clean up
            $client.Disconnect()
        }    
    }
    catch [Exception]
    {
      echo $_.Exception|format-list -force
      
    }
}
<#
 .Synopsis
  Get a file from an FTP folder.

 .Description
  Connect to a FTP site and lis the contents

 .Parameter Site
  The site to connect to.

 .Parameter User
  The user name.

 .Parameter Password
  Password associated with FTP site.

 .Parameter FtpDirectory
  The Directory on FTP server
  
  .Parameter FtpfileName
  Filename to transfer
  
 .Example
   # Get a file from FTP
   Get-FtpFile -Site ftp.site.com -User bob -Password secure -FtpDirectory pub -ftpfileName "Read*"

#>
function Get-FtpFile {
param(
    [Parameter(Mandatory=$true)]
    [string] $site,
    [Parameter(Mandatory=$true)]
    [string] $user,
    [Parameter(Mandatory=$true)]
    [string] $password,
    [string] $ftpDirectory = "",
    [Parameter(Mandatory=$true)]
    [string] $ftpFileName,
    [switch] $binary = $false
    )

    try
    {
        
        # Load FluentFTP .NET assembly
        Add-Type -Path "$PSScriptRoot\FluentFTP.dll"
        # Setup session options
        $client = New-Object FluentFTP.FtpClient($site)
        $client.Credentials = New-Object System.Net.NetworkCredential($user, $password)
        if ($binary)
        {
            $dataType = "Binary"
        }
        else
        {
            $dataType = "ASCII"
        }
            
        $client.DownloadDatatype = $dataType
        $client.AutoConnect()

        try
        {
            if ($ftpdirectory -ne "")
            {
                $client.SetWorkingDirectory($ftpDirectory)
            }
            
            $currentDirectory = $client.GetWorkingDirectory()  
            $filesDownloaded = $false            
            foreach ($item in $client.GetListing(""))
            {
                $fileonly = $item.Name
                $localFile = Join-path $pwd $fileOnly
                if ($item.Name -like $ftpFileName)
				{
					if ($client.DownloadFile($localFile, $fileOnly))
					{
                        $filesDownloaded = $true
						Write-Output ("$fileOnly successfully downloaded to $localFile" )
					}
					else
					{
						Write-Output ("Unable to download $fileOnly to $localFile" )
					}
				}
            }
            if (!$filesDownloaded)
            {
                Write-Output "Attempting to download files matching $ftpFileName. No files were found"
            }
        }    
        finally
        {
            # Disconnect, clean up
            $client.Disconnect()
        }            
    }
    catch [Exception]
    {
      echo $_.Exception|format-list -force
      
    }
}
<#
 .Synopsis
  Delete a file from an FTP folder.

 .Description
  Connect to a FTP site and lis the contents

 .Parameter Site
  The site to connect to.

 .Parameter User
  The user name.

 .Parameter Password
  Password associated with FTP site.

 .Parameter FtpDirectory
  The Directory on FTP server
  
  .Parameter ftpfileName
  Filename to delete
  
 .Example
   # Delete a file or files from an FTP folder
   Remove-FtpFile -Site ftp.site.com -User bob -Password secure -FtpDirectory pub -ftpfileName "Read*"

#>
function Remove-FtpFile {
param(
    [Parameter(Mandatory=$true)]
    [string] $site,
    [Parameter(Mandatory=$true)]
    [string] $user,
    [Parameter(Mandatory=$true)]
    [string] $password,
    [string] $ftpDirectory = "",
    [Parameter(Mandatory=$true)]
    [string] $ftpFileName
    )

    try
    {
        
        # Load FluentFTP .NET assembly
        Add-Type -Path "$PSScriptRoot\FluentFTP.dll"
        # Setup session options
        $client = New-Object FluentFTP.FtpClient($site)
        $client.Credentials = New-Object System.Net.NetworkCredential($user, $password)
        $client.AutoConnect()

        try
        {
            if ($ftpDirectory -ne "")
            {
                $client.SetWorkingDirectory($ftpDirectory)
            }
            $filesDeleted = $false         
            foreach ($item in $client.GetListing(""))
            {
                $fileOnly = $item.Name
				if ($fileonly -like $ftpFileName)
				{
                    $filesDeleted = $true
					$success = $client.DeleteFile($fileOnly)
					Write-Output "$fileOnly successfully deleted"
				}
            }
            if (!$filesDeleted)
            {
                Write-Output "No files matching $ftpFileName were found on the FTP server"
            }
        }    
        finally
        {
            # Disconnect, clean up
            $client.Disconnect()
        }            
    }
    catch [Exception]
    {
      echo $_.Exception|format-list -force
      
    }
}