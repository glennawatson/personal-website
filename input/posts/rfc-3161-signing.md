---
Title: Using RFC3161 signing with Sms
Published: 31/05/2019
Tags:
- RFC3161
- Signing
- x509
- C#
---

I recently decided to make my own signing application for git. As part of this I wanted to perform RFC 3161 signing against my signature.

The .NET Core Fx team released RFC 3161 support in .NET Core in April 2019. It is worth noting that the RFC3161 classes only exist in the .NETCoreApp2.1 or above. .NET Standard 2.0 is not supported but support is coming in .NET Standard 2.1.

This article will cover how to use the new RFC 3161 systems, and using the existing `SignedCms` class to sign a message. It will also cover verifying a message once it's decoded.

Thanks to [Jeremy Barton](https://github.com/bartonjs) who assisted in understanding elements of this process.

Check out my [SMimeSigner](https://github.com/glennawatson/smimesigner) project for a example of the code below. This project allows you to use `GPG` verify and sign functionality with the X509 certificate stores. It's similar to the GitHub [smimesign](https://github.com/github/smimesign).

# Overview

The CoreFX team have deliberately left out how to communicate RFC3161 requests to the server, but have the ability to generate the requests and parse the responses. This is because the CoreFX team have no knowledge of WebRequests or HttpClient at this level.

At the moment it's a little undocumented how to use this API; most of the examples I found online refer to the bouncy castle framework.

## Signing

So to process a RFC3161 request the process would be as follows:

1. Use a `SignedCms` for the contents of your signing request.
2. Set the certificate on a `CmsSigner`.
3. Add a Pkcs7 signing time to the request.
4. Compute the signature using the `CmsSigner`.
5. Generate a 'nonce'.
6. Create a `Rfc3161TimestampRequest` which we will pass to our HttpClient.
7. Process the response, using the `Rfc3161TimestampRequest` object.
8. Add to the unsigned attributes the Signature Timestamp we received.

In this article I intend to take you through all the steps above.

### Create a SignedCms and CmsSigner

So somewhere you will need to get a `X509Certificate2` object. In my personal code I got this from the user store from a selected thumbprint.

```cs
var contentInfo = new ContentInfo(bytesToSign);
var cms = new SignedCms(contentInfo, isDetached);
var signer = new CmsSigner(certificate) { IncludeOption = X509IncludeOption.WholeChain };
signer.SignedAttributes.Add(new Pkcs9SigningTime());
```

In this example, the contents we want to sign are in bytes (could be a stream also). We use optionally detached timing and set a TimeStamp in the signer. This is different from the timestamp certificate we will get from the RFC3161 authority.

### Compute the signature

```cs
cms.ComputeSignature(signer, false);
```

We will compute the signature BEFORE we request a RFC3161. This is important since the timestamp authority will timestamp against your signed contents.

### Generate our Nonce

We need to generate a random nonce. I am just using the example they had in the [.NET core issue they used for introducing this API](https://github.com/dotnet/corefx/issues/24524).

```cs
byte[] nonce = new byte[8];

using (var rng = RandomNumberGenerator.Create())
{
    rng.GetBytes(nonce);
}
```

### Get our signing information and create the RFC3161 request

We need to get the signing information from our signer. We often will just have a bit of validation to confirm that there is only one lot of signing information.

```cs
SignerInfo newSignerInfo = cms.SignerInfos[0];
```

Now we generate our request for us to send to our RFC3161 signing authority.

```cs
var request = Rfc3161TimestampRequest.CreateFromSignerInfo(
    newSignerInfo,
    HashAlgorithmName.SHA384,
    requestSignerCertificates: true,
    nonce: nonce);
```

### Send our request to the RFC3161 authority

You can use your own web request system, in this example we are just going to use a `HttpClient` class.

```cs
var client = new HttpClient();
var content = new ReadOnlyMemoryContent(request.Encode());
content.Headers.ContentType = new MediaTypeHeaderValue("application/timestamp-query");
var httpResponse = await client.PostAsync(timeStampAuthorityUri, content).ConfigureAwait(false);
```

### Process our response

The `Rfc3161TimestampRequest` has a helper method `ProcessResponse` which assists in processing a response from a server.

```cs
if (!httpResponse.IsSuccessStatusCode)
{
    throw new CryptographicException(
        $"There was a error from the timestamp authority. It responded with {httpResponse.StatusCode} {(int)httpResponse.StatusCode}: {httpResponse.Content}");
}

if (httpResponse.Content.Headers.ContentType.MediaType != "application/timestamp-reply")
{
    throw new CryptographicException("The reply from the time stamp server was in a invalid format.");
}

var data = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

var timestampToken = request.ProcessResponse(data, out _);
```

### Add the request to our unsigned attributes

Because the RFC3161 sign certificate is separate to the contents that was signed, we need to add it to the unsigned attributes.

```cs
Oid SignatureTimeStampOin { get; } = new Oid("1.2.840.113549.1.9.16.2.14");

newSignerInfo.UnsignedAttributes.Add(new AsnEncodedData(SignatureTimeStampOin, timestampToken.AsSignedCms().Encode()));
```

## Verifying our signature

Below is the work flow for verifying the signature. Most of the steps will be the same as the existing `SignedCms` class verification, with an additional stamp to jump in and read the RFC3161 timestamp.

### Create our SignedCms

If you are creating a detached signature check, pass in a ContentInfo to your SignedCms instance, otherwise just use the empty constructor

In the example below we assume detached if the signatureBytes is not null.
For detached:
```cs
// Create our signer.
SignedCms signedCms;
if (signatureBytes != null)
{
    var contentInfo = new ContentInfo(signatureBytes);
    signedCms = new SignedCms(contentInfo, true);
}
else
{
    signedCms = new SignedCms();
}
```

### Decode and verify the signature

We are assuming your document bytes are being passed in below. This is very similar to off the shelf verification. We are assuming you want to verify the certificate also.

```cs
// Decode the body we want to check.
signedCms.Decode(body);

// Check the signature, true indicates we want to check the validity against certificate authorities.
signedCms.CheckSignature(true);

if (signedCms.SignerInfos.Count == 0)
{
    throw new CryptographicException("Must have valid signing information. There is none in the signature.");
}
```

### Verify the RFC3161 timestamp if attached.

In this step we are now going to finally get to verifying our RFC3161 timestamps.

We will check each signing block for certificates and verify they are within the specified date ranges.

```cs
foreach (var signedInfo in signedCms.SignerInfos)
{
    if (CheckRFC3161Timestamp(signedInfo, signedInfo.Certificate.NotBefore, signedInfo.Certificate.NotAfter) == false)
    {
        throw new CryptographicException("The RFC3161 timestamp is invalid.");
    }
}

bool? CheckRFC3161Timestamp(SignerInfo signerInfo, DateTimeOffset? notBefore, DateTimeOffset? notAfter)
{
    bool found = false;
    byte[] signatureBytes = null;

    foreach (CryptographicAttributeObject attr in signerInfo.UnsignedAttributes)
    {
        if (attr.Oid.Value == SignatureTimeStampOin.Value)
        {
            foreach (AsnEncodedData attrInst in attr.Values)
            {
                byte[] attrData = attrInst.RawData;

                // New API starts here:
                if (!Rfc3161TimestampToken.TryDecode(attrData, out var token, out var bytesRead))
                {
                    return false;
                }

                if (bytesRead != attrData.Length)
                {
                    return false;
                }

                signatureBytes = signatureBytes ?? signerInfo.GetSignature();

                // Check that the token was issued based on the SignerInfo's signature value
                if (!token.VerifySignatureForSignerInfo(signerInfo, out _))
                {
                    return false;
                }

                var timestamp = token.TokenInfo.Timestamp;

                // Check that the signed timestamp is within the provided policy range
                // (which may be (signerInfo.Certificate.NotBefore, signerInfo.Certificate.NotAfter);
                // or some other policy decision)
                if (timestamp < notBefore.GetValueOrDefault(timestamp) ||
                    timestamp > notAfter.GetValueOrDefault(timestamp))
                {
                    return false;
                }

                var tokenSignerCert = token.AsSignedCms().SignerInfos[0].Certificate;

                // Implicit policy decision: Tokens required embedded certificates (since this method has
                // no resolver)
                if (tokenSignerCert == null)
                {
                    return false;
                }

                found = true;
            }
        }
    }

    // If we found any attributes and none of them returned an early false, then the SignerInfo is
    // conformant to policy.
    if (found)
    {
        return true;
    }

    // Inconclusive, as no signed timestamps were found
    return null;
}
```

## Putting it all together

```cs
public static class SignatureUtilities
{
    public static Oid SignatureTimeStampOin { get; } = new Oid("1.2.840.113549.1.9.16.2.14");

    public static async Task<byte[]> SignWithRfc3161(byte[] bytesToSign, bool isDetached, X509Certificate2 certificate, Uri timeStampAuthorityUri)
    {
        // Sign our contents.
        var contentInfo = new ContentInfo(bytesToSign);
        var cms = new SignedCms(contentInfo, isDetached);
        var signer = new CmsSigner(certificate) { IncludeOption = X509IncludeOption.WholeChain };
        signer.SignedAttributes.Add(new Pkcs9SigningTime());

        cms.ComputeSignature(signer, false);

        // Generate our nonce
        byte[] nonce = new byte[8];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        // Get our signing information and create the RFC3161 request
        SignerInfo newSignerInfo = cms.SignerInfos[0];

        // Now we generate our request for us to send to our RFC3161 signing authority.
        var request = Rfc3161TimestampRequest.CreateFromSignerInfo(
            newSignerInfo,
            HashAlgorithmName.SHA384,
            requestSignerCertificates: true,
            nonce: nonce);

        // You can use your own web request system, in this example we are just going to use a `HttpClient` class.
        var client = new HttpClient();
        var content = new ReadOnlyMemoryContent(request.Encode());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/timestamp-query");
        var httpResponse = await client.PostAsync(timeStampAuthorityUri, content).ConfigureAwait(false);

        // Process our response
        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new CryptographicException(
                $"There was a error from the timestamp authority. It responded with {httpResponse.StatusCode} {(int)httpResponse.StatusCode}: {httpResponse.Content}");
        }

        if (httpResponse.Content.Headers.ContentType.MediaType != "application/timestamp-reply")
        {
            throw new CryptographicException("The reply from the time stamp server was in a invalid format.");
        }

        var data = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

        var timestampToken = request.ProcessResponse(data, out _);

        // The RFC3161 sign certificate is separate to the contents that was signed, we need to add it to the unsigned attributes.
        newSignerInfo.UnsignedAttributes.Add(new AsnEncodedData(SignatureTimeStampOin, timestampToken.AsSignedCms().Encode()));

        return cms.Encode();
    }

    public static void Verify(byte[] body, byte[] signatureBytes)
    {
        // Create our signer.
        SignedCms signedCms;
        if (signatureBytes != null)
        {
            var contentInfo = new ContentInfo(signatureBytes);
            signedCms = new SignedCms(contentInfo, true);
        }
        else
        {
            signedCms = new SignedCms();
        }

        // Decode the body we want to check.
        signedCms.Decode(body);

        // Check the signature, true indicates we want to check the validity against certificate authorities.
        signedCms.CheckSignature(true);

        if (signedCms.SignerInfos.Count == 0)
        {
            throw new CryptographicException("Must have valid signing information. There is none in the signature.");
        }

        foreach (var signedInfo in signedCms.SignerInfos)
        {
            if (CheckRFC3161Timestamp(signedInfo, signedInfo.Certificate.NotBefore, signedInfo.Certificate.NotAfter) == false)
            {
                throw new CryptographicException("The RFC3161 timestamp is invalid.");
            }
        }
    }

    internal static bool? CheckRFC3161Timestamp(SignerInfo signerInfo, DateTimeOffset? notBefore, DateTimeOffset? notAfter)
    {
        bool found = false;
        byte[] signatureBytes = null;

        foreach (CryptographicAttributeObject attr in signerInfo.UnsignedAttributes)
        {
            if (attr.Oid.Value == SignatureTimeStampOin.Value)
            {
                foreach (AsnEncodedData attrInst in attr.Values)
                {
                    byte[] attrData = attrInst.RawData;

                    // New API starts here:
                    if (!Rfc3161TimestampToken.TryDecode(attrData, out var token, out var bytesRead))
                    {
                        return false;
                    }

                    if (bytesRead != attrData.Length)
                    {
                        return false;
                    }

                    signatureBytes = signatureBytes ?? signerInfo.GetSignature();

                    // Check that the token was issued based on the SignerInfo's signature value
                    if (!token.VerifySignatureForSignerInfo(signerInfo, out _))
                    {
                        return false;
                    }

                    var timestamp = token.TokenInfo.Timestamp;

                    // Check that the signed timestamp is within the provided policy range
                    // (which may be (signerInfo.Certificate.NotBefore, signerInfo.Certificate.NotAfter);
                    // or some other policy decision)
                    if (timestamp < notBefore.GetValueOrDefault(timestamp) ||
                        timestamp > notAfter.GetValueOrDefault(timestamp))
                    {
                        return false;
                    }

                    var tokenSignerCert = token.AsSignedCms().SignerInfos[0].Certificate;

                    // Implicit policy decision: Tokens required embedded certificates (since this method has
                    // no resolver)
                    if (tokenSignerCert == null)
                    {
                        return false;
                    }

                    found = true;
                }
            }
        }

        // If we found any attributes and none of them returned an early false, then the SignerInfo is
        // conformant to policy.
        if (found)
        {
            return true;
        }

        // Inconclusive, as no signed timestamps were found
        return null;
    }
}
```
