Title: Using a code signing certificate with Github And Yubikey
Published: 31/05/2019
Tags:
- Yubikey
- Code Signing
---

So I was using GPG key to generate perform commit signing verification with GitHub based on the work from Geoffrey Huntley. He has a excellent [post](https://ghuntley.com/notes/git/) on using GPG.

This article explores using x509 certificates as verification sources and Yubikey.

This following guide will only work with Yubikey devices that have PIV support. You may be able to adapt this guide to other PIV enabled security devices.  

You will need to get a code signing certificate from a verified certificate authority. GitHub uses the Debian CA certificate package to verify the SMime Certificate which is the same used by the Mozilla browser. I found DigiCert very good, they were able to do Skype notary public services for free, since notary services are expensive in Australia.

In this guide all the instructions are for windows but they can be adapted for other platforms with some modifications.

## What's the motivation

**So what's the motivation of doing commit signing for git?**

- People know that it's someone with access to your private key, and therefore ideally it's you.
- GitHub will show that it's signed, so people start to build their confidence. With authorized certificate authorities GitHub will show the authority details in the verification.
- Using a certificate authority the user knows that you have gone through physical ID checks to be issued the certificate.

**What's the motivation for using a YubiKey?**

- Greater security for storing your private key. It gets stored on the YubiKey device and the only way to access it is with a passcode which must be entered once per session.
- Optionally YubiKey's come with a touch button which generates a hash sequence.
- Yubikey can be used for 2FA for GitHub website as well so you get extra security there also.

## Yubikey Setup

First step is to get a X%09 certificate from a authorised provider such as DigiCert, making sure your CSR request contains your GitHub email. Follow their instructions on how install and use the key.

On windows you can use a [Yubikey Mini Smart Driver](https://support.yubico.com/support/solutions/articles/15000006456-yubikey-smart-card-deployment-guide#YubiKey_Minidriver_Installationies8o) but I found the YubiKey manager approach detailed below easier.

I am assuming a pin policy of "once" per session, and no "touch" policy, there are other [options](https://support.yubico.com/support/solutions/articles/15000012643-yubikey-manager-cli-ykman-user-manual#ykman_piv_import-keyk8p1yl). I am also installing into slot 9c which is the signing slot.  

1. Export out a PFX file from the X509 certificate. Make a backup in a safe location of this file, if someone gets it they can pretend to be you.
1. Install the [YubiKey manager](https://developers.yubico.com/yubikey-manager-qt/).
1. Open a command line.
1. Run `cd "%PROGRAMFILES%\Yubico\YubiKey Manager"`
1. Change your pin from the default (if you haven't already) and change from the default pin 123456. Run `.\ykman piv change-pin -P 123456 -n <new pin>`
1. Run: `.\ykman piv import-key --pin-policy=default 9d C:\path\to\your.pfx`
1. When prompted, enter the PIN, management key, and password for the PFX.
1. Run: `.\ykman piv import-certificate 9d C:\path\to\your.pfx`
1. When prompted, enter the PIN, management key, and password for the PFX.
1. You may need to logout of your profile if the keys don't show up in SMIMESign below.

## GitHub setup

I am going to assume you want to use S/MIME signing for all your repositories. There are other options in the [GitHub guide](https://help.github.com/en/articles/telling-git-about-your-signing-key#telling-git-about-your-x509-key-1).
We are going to generate a batch file to run smimedesign due to a ongoing [issue](https://github.com/github/smimesign/issues/47) with timestamp authorities.

1. Install [S/MIME sign](https://github.com/github/smimesign#windows).
1. Create a batch file that is in your path to smimedesign. For windows the batch file sample (which you could name `sign.cmd`)
   ```batchfile
   @echo off
   smimesign.exe --timestamp-authority http://timestamp.digicert.com %*
   ```
1. Tell git to use smimesign. From the command line run:
   1. `git config --global gpg.x509.program c:/path/to/sign.cmd`
   1. `git config --global gpg.format x509`
   1. `git config --global commit.gpgsign true`
1. Find the key you want to sign with. Run `smimesign --list-keys` and find the serial number of the key you wish to sign with. Copy the serial number of the key so you can paste it into the next command. Make sure you use the serial number not the ID.
1. From the command line set the key to use `git config --global user.signingkey a2dfa7e8c9c4d1616f1009c988bb70f`

Now each time you commit it will ask you for a passcode that you set earlier.

## Acknowledgements

- Part of this guide is based off the [Yubico Support Pages](https://support.yubico.com/support/solutions/articles/15000006474-code-signing-with-the-yubikey-on-windows).
- I originally got a lot of help from Geoffrey Huntley and his [notes](https://ghuntley.com/notes/git/).
- Part of this guide is based off the [Github Signing Article](https://help.github.com/en/articles/telling-git-about-your-signing-key).
- I also heard about smimedesign from [Oren Novotny](https://twitter.com/onovotny).
