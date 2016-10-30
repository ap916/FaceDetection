using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Diagnostics;
using Windows.Storage.Pickers;
using Windows.Media.SpeechSynthesis;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FaceDetection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("Authentication_Key");

        public MainPage()
        {
            this.InitializeComponent();
            //CreatePersonGroup();
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "Find Familiar Faces";
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(1280, 720);

            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User cancelled photo capture
                return;
            }

            IRandomAccessStream stream = await photo.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            CaptureImage.Source = bitmapSource;

            TitleText.Text = "Detecting Faces ...";
            // Detecting the face 
            string personGroupId = "myfriends";
            string AllNames = "Detected : ";
            using (Stream s = File.OpenRead(photo.Path))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                if (faceIds.Length == 0)
                {
                    AllNames = "No faces found";
                    TitleText.Text = AllNames;                    
                }
                else {
                    var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                    foreach (var identifyResult in results)
                    {
                        //Console.WriteLine("Result of face: {0}", identifyResult.FaceId);

                        if (identifyResult.Candidates.Length == 0)
                        {
                            //Console.WriteLine("No one identified");
                            AllNames += "Unknown, ";
                        }
                        else
                        {
                            // Get top 1 among all candidates returned
                            var candidateId = identifyResult.Candidates[0].PersonId;
                            var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                            //Console.WriteLine("Identified as {0}", person.Name);
                            AllNames += person.Name + " ";
                        }
                    }
                }
            }
            TitleText.Text = AllNames;
            readText(AllNames);
            await photo.DeleteAsync();
        }


        // Function for Creating Group and Training the data
        // Run required only one time
        private async void CreatePersonGroup()
        {
            // Create an empty person group
            string personGroupId = "myfriends";
            await faceServiceClient.CreatePersonGroupAsync(personGroupId, "MyFriends");

            // Define Manas
            CreatePersonResult friend1 = await faceServiceClient.CreatePersonAsync(
                // Id of the person group that the person belonged to
                personGroupId,
                // Name of the person
                "Manas"
            );
            // Define Ameya
            CreatePersonResult friend2 = await faceServiceClient.CreatePersonAsync(
                // Id of the person group that the person belonged to
                personGroupId,
                // Name of the person
                "Ameya"
            );
            // Define Shubhankar
            CreatePersonResult friend3 = await faceServiceClient.CreatePersonAsync(
                // Id of the person group that the person belonged to
                personGroupId,
                // Name of the person
                "Shubhankar"
            );
            
            // Directory contains image files of Manas            
            const string friend1ImageDir = @"D:\Pictures\MyFriends\Manas\";

            foreach (string imagePath in Directory.GetFiles(friend1ImageDir, "*.jpg"))
                {
                TitleText.Text = imagePath;
                using (Stream s = File.OpenRead(imagePath))
                {
                    // Detect faces in the image and add to Anna
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, friend1.PersonId, s);
                    
                }
            }

            // Directory contains image files of Ameya
            const string friend2ImageDir = @"D:\Pictures\MyFriends\Ameya\";

            foreach (string imagePath in Directory.GetFiles(friend2ImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    // Detect faces in the image and add to Anna
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, friend2.PersonId, s);
                }
            }

            // Directory contains image files of Manas
            const string friend3ImageDir = @"D:\Pictures\MyFriends\Shubhankar\";

            foreach (string imagePath in Directory.GetFiles(friend3ImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    // Detect faces in the image and add to Anna
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, friend3.PersonId, s);
                }
            }

            TitleText.Text = "Train about to start";
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);
            TitleText.Text = "Train started";

            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);
                int count = 0;
                if (trainingStatus.Status.ToString() != "running")
                {
                    break;
                }
                else
                {
                    count++;
                }
                TitleText.Text = count.ToString();
                await Task.Delay(1000);
            }
            TitleText.Text = "Finished";

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // CreatePersonGroup() is called when we need to upload data to server and train it.
            // Required only one time.
            //CreatePersonGroup();
        }

        // Function for reading the text passed to it
        private async void readText(string mytext)
        {
            MediaElement mediaplayer = new MediaElement();
            using (var speech = new SpeechSynthesizer())
            {
                speech.Voice = SpeechSynthesizer.AllVoices.First(gender => gender.Gender == VoiceGender.Female);
                string ssml = @"<speak version='1.0' " + "xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" + mytext + "</speak>";
                SpeechSynthesisStream stream = await speech.SynthesizeSsmlToStreamAsync(ssml);
                mediaplayer.SetSource(stream, stream.ContentType);
                mediaplayer.Play();
            }
        }
    }
}
