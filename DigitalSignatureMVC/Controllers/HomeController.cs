using DigitalSignatureMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;
using Syncfusion.Pdf.Redaction;
using Syncfusion.Pdf.Interactive;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Syncfusion.Drawing;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DigitalSignatureMVC.Controllers
{
    public class HomeController : Controller
    {
        
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult FileUpload()
        {
            return View();
        }
        public IActionResult FileServer()
        {
            return View();
        }
   
        //public ActionResult Position()
        //{
        //    ViewBag.data = new string[] { "Top Right", "Middle Right", "Bottom Right", "Top Middle", "Middle", "Bottom Middle", "Top Left", "Middle Left", "Bottom Left" };
        //    return View();
        //}
        //public ActionResult Imagesize()
        //{
        //    ViewBag.value = new string[] { "Large", "Medium", "Small" };
        //    return View();
        //}

        [HttpPost]
        public ActionResult DigitalSignature(string password, string Reason, string Location, string Contact, string submit, string Cryptographic, string Digest_Algorithm, IFormFile pdfdocument, IFormFile certificate, IFormFile sign, string ipage, string position, string imagesize)
        {
            if (submit == "Create Sign PDF")
            {
                if (pdfdocument != null && pdfdocument.Length > 0 && certificate != null && certificate.Length > 0 && sign != null && sign.Length > 0 && (certificate.FileName.Contains(".pfx") || certificate.FileName.Contains(".p12") || certificate.FileName.Contains(".hsm") || certificate.FileName.Contains(".ocsp") || certificate.FileName.Contains(".crl") || certificate.FileName.Contains(".ecdsa")) 
                    && password != null && Location != null && Reason != null && Contact != null && Request.Form.Files != null && Request.Form.Files.Count != 0)
                {

                    int ipageInt = int.Parse(ipage);

                    PdfLoadedDocument ldoc = new PdfLoadedDocument(pdfdocument.OpenReadStream());
                    PdfCertificate pdfCert = new PdfCertificate(certificate.OpenReadStream(), password);
                    PdfImage image = PdfImage.FromStream(sign.OpenReadStream());

                    if (ipage != null && position != null && imagesize != null )
                    {
                        PdfLoadedPage lpage = ldoc.Pages[ipageInt-1] as PdfLoadedPage;
                        PdfSignature signature = new PdfSignature(ldoc, lpage, pdfCert, "Signature");

                        signature.TimeStampServer = new TimeStampServer(new Uri("http://timestamp.digicert.com/"));
                        Set_PositionandSize(position, imagesize, signature, image);
                        signature.ContactInfo = Contact;
                        signature.LocationInfo = Location;
                        signature.Reason = Reason;
                        
                        SetCryptographicStandard(Cryptographic, signature);
                        SetDigest_Algorithm(Digest_Algorithm, signature);

                    }
                    else
                    {
                        ViewBag.lab = "Fill proper redaction bounds to redact";
                    }

                    MemoryStream stream = new MemoryStream();
                    ldoc.Save(stream);
                    stream.Position = 0;
                    ldoc.Close(true);

                    FileStreamResult fileStreamResult = new FileStreamResult(stream, "application/pdf");
                    //fileStreamResult.LastModified = DateTimeOffset.Now;
                    fileStreamResult.FileStream = stream;
                    //fileStreamResult.FileDownloadName = "SignedPDF.pdf";
                    return fileStreamResult;
                }
                else
                {
                    ViewBag.lab = "NOTE: Fill all fields and then create PDF";
                    return View();
                }
            }
            else
            {
                ViewBag.lab = "NOTE: Fill all fields and then create PDF";
                return View();
            }
        }

        //Upload file from server
        [HttpPost]
        public ActionResult FileServer(string streamReason, string streamLocation, string streamContact, string streamsubmit, string streamCryptographic, string streamDigest_Algorithm, string streamipage, string x, string y, string width, string height)
        {
            if (streamsubmit == "Create Sign PDF")
            {
                float x1 = float.Parse(x);
                float y1 = float.Parse(y);
                float width1 = float.Parse(width);
                float height1 = float.Parse(height);

                Stream file = new FileStream("Upload/TM.pdf", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                Stream cer = new FileStream("Upload/Certificates.p12", FileMode.Open, FileAccess.Read, FileShare.Read);
                Stream img = new FileStream("Upload/signImg.png", FileMode.Open);

                int ipageInt = int.Parse(streamipage);
                PdfLoadedDocument doc = new PdfLoadedDocument(file);
                PdfCertificate certificate = new PdfCertificate(cer, "pass@word1");
                PdfSignature signature = new PdfSignature(doc, doc.Pages[ipageInt - 1], certificate, "DigitalSignature");
                PdfImage image = PdfImage.FromStream(img);

                signature.TimeStampServer = new TimeStampServer(new Uri("http://timestamp.digicert.com/"));
                signature.Bounds = new RectangleF(x1, y1, width1, height1);
                signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                //Set_PositionandSize(streamposition, streamimagesize, signature, image);

                signature.ContactInfo = streamContact;
                signature.LocationInfo = streamLocation;
                signature.Reason = streamReason;

                SetCryptographicStandard(streamCryptographic, signature);
                SetDigest_Algorithm(streamDigest_Algorithm, signature);

                doc.Save(file);
                doc.Close(true);

                return View();
            }
            else
            {
                ViewBag.lab = "NOTE: Fill all fields and then create PDF";
                return View();
            }
        }

        public void SetCryptographicStandard(string cryptographic, PdfSignature signature)
        {
            if (cryptographic != null)
            {
                if (cryptographic == "CAdES")
                    signature.Settings.CryptographicStandard = CryptographicStandard.CADES;
                else
                    signature.Settings.CryptographicStandard = CryptographicStandard.CMS;
            }

        }
        public void SetDigest_Algorithm(string digest_Algorithm, PdfSignature signature)
        {
            if (digest_Algorithm != null)
            {
                switch (digest_Algorithm)
                {
                    case "SHA1":
                        signature.Settings.DigestAlgorithm = DigestAlgorithm.SHA1;
                        break;
                    case "SHA384":
                        signature.Settings.DigestAlgorithm = DigestAlgorithm.SHA384;
                        break;
                    case "SHA512":
                        signature.Settings.DigestAlgorithm = DigestAlgorithm.SHA512;
                        break;
                    case "RIPEMD160":
                        signature.Settings.DigestAlgorithm = DigestAlgorithm.RIPEMD160;
                        break;
                    default:
                        signature.Settings.DigestAlgorithm = DigestAlgorithm.SHA256;
                        break;
                }
            }
        }
        public void Set_PositionandSize(string position, string size, PdfSignature signature, PdfImage image)
        {
            float width1 = 0;
            float height1 = 0;

            if (position != null && size != null)
            {
                if (size == "Large")
                {
                    width1 = 200;
                    height1 = 100;
                    switch (position)
                    {
                        case "Top Right":
                            signature.Bounds = new RectangleF(new PointF(360, 30), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle Right":
                            signature.Bounds = new RectangleF(new PointF(360, 371), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Right":
                            signature.Bounds = new RectangleF(new PointF(360, 700), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Top Middle":
                            signature.Bounds = new RectangleF(new PointF(198, 30), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle":
                            signature.Bounds = new RectangleF(new PointF(198, 371), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Middle":
                            signature.Bounds = new RectangleF(new PointF(198, 700), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Top Left":
                            signature.Bounds = new RectangleF(new PointF(30, 30), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle Left":
                            signature.Bounds = new RectangleF(new PointF(30, 371), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Left":
                            signature.Bounds = new RectangleF(new PointF(30, 700), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        default:
                            signature.Bounds = new RectangleF(new PointF(360, 700), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                    }
                }
                else if (size == "Mediem")
                {
                    width1 = 150;
                    height1 = 75;
                    switch (position)
                    {
                        case "Top Right":
                            signature.Bounds = new RectangleF(new PointF(400, 40), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle Right":
                            signature.Bounds = new RectangleF(new PointF(400, 383), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Right":
                            signature.Bounds = new RectangleF(new PointF(400, 730), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Top Middle":
                            signature.Bounds = new RectangleF(new PointF(223, 40), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle":
                            signature.Bounds = new RectangleF(new PointF(223, 383), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Middle":
                            signature.Bounds = new RectangleF(new PointF(223, 730), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Top Left":
                            signature.Bounds = new RectangleF(new PointF(40, 40), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle Left":
                            signature.Bounds = new RectangleF(new PointF(40, 383), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Left":
                            signature.Bounds = new RectangleF(new PointF(40, 730), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        default:
                            signature.Bounds = new RectangleF(new PointF(400, 730), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                    }
                }
                else if (size == "Small")
                {
                    width1 = 100;
                    height1 = 50;
                    switch (position)
                    {
                        case "Top Right":
                            signature.Bounds = new RectangleF(new PointF(440, 45), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle Right":
                            signature.Bounds = new RectangleF(new PointF(440, 396), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Right":
                            signature.Bounds = new RectangleF(new PointF(440, 750), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Top Middle":
                            signature.Bounds = new RectangleF(new PointF(248, 45), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle":
                            signature.Bounds = new RectangleF(new PointF(248, 396), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Middle":
                            signature.Bounds = new RectangleF(new PointF(248, 750), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Top Left":
                            signature.Bounds = new RectangleF(new PointF(45, 45), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Middle Left":
                            signature.Bounds = new RectangleF(new PointF(45, 396), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        case "Bottom Left":
                            signature.Bounds = new RectangleF(new PointF(45, 750), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                        default:
                            signature.Bounds = new RectangleF(new PointF(440, 750), new SizeF(width1, height1));
                            signature.Appearance.Normal.Graphics.DrawImage(image, 0, 0, width1, height1);
                            break;
                    }
                }
                
            }
        }

    }
}
