using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
//Need to use these for google
using Google.Impl;
using Google;
using UnityEngine.Purchasing;
using BrainCloud.JsonFx.Json;

public class BrainCloudInterface : MonoBehaviour, IStoreListener //needed for unity iap
{
    //these are simply references to the unity specific canvas system
    Text status;
    string statusText;
    string email;
    string authCode;
    string idToken;

    //purchase
    string productId;
    string orderId;
    string purchaseToken;
    string developerPayload;

    //Google info 
    Dictionary<string, object> wrapper;
    string store;
    string payload;
    Dictionary<string, object> gpDetails;
    string gpJson;
    string gpSig;

    GoogleSignInConfiguration configuration;
    //the webClientId of our googleOpenId test app. To test your own app, enter in your apps own webClientId
    string webClientId = "780423637529-hcg9gdbo4egbbh7h6k9pkukd8a9j9f8u.apps.googleusercontent.com";

    //for purchasing
    private static IStoreController m_StoreController;          // The Unity Purchasing system.
    private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.
    public static string kProductIDConsumable = "bc_google_orb1";

    // Use this for initialization
    void Start()
    {
        //allow the people who sign in to change profiles. 
        BCConfig._bc.SetAlwaysAllowProfileSwitch(true);
        BCConfig._bc.Client.EnableLogging(true);

        //unity's ugly way to look for gameobjects
        status = GameObject.Find("Status").GetComponent<Text>();

        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestEmail = true,
            RequestIdToken = true,
            //auth code is not needed for OpenId authentication
            RequestAuthCode = true
        };


        // If we haven't set up the Unity Purchasing reference
        if (m_StoreController == null)
        {
            // Begin to configure our connection to Purchasing
            InitializePurchasing();
        }

    }

    void Update()
    {
        status.text = statusText;
    }

    //google sign in

    public void OnGoogleSignIn()
    {
        //set the google configuration to the configuration object you set up
        GoogleSignIn.Configuration = configuration;
        
        //Can define this
        GoogleSignIn.Configuration.UseGameSignIn = false;

        //Can also define these tags here like this.
        //GoogleSignIn.Configuration.RequestEmail = true;
        //GoogleSignIn.Configuration.RequestIdToken = true;
        //GoogleSignIn.Configuration.RequestAuthCode = true;

        statusText = "Calling Sign in";
        // With the configuration set, its now time to start trying to sign in. Pass in a callback to wait for success. 
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleAuthSignIn);
    }

    public void OnAuthBraincloudOpenId()
    {
        BCConfig._bc.AuthenticateGoogleOpenId(email, idToken, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void OnSuccess_Authenticate(string responseData, object cbObject)
    {
        statusText = "Logged into braincloud!\n" + responseData;
    }

    public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        statusText = "Failed to Login to braincloud...\n" + statusMessage + "\n" + reasonCode;        
    }

    //use a callback with a task to easily get results from the callback in order to get the values you need. 
    public void OnGoogleAuthSignIn(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
            using (IEnumerator<System.Exception> enumerator =
                    task.Exception.InnerExceptions.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    GoogleSignIn.SignInException error =
                            (GoogleSignIn.SignInException)enumerator.Current;
                    statusText = "Got Error: " + error.Status + " " + error.Message;
                }
                else
                {
                    statusText = "Got Unexpected Exception?!?" + task.Exception;
                }
            }
        }
        else if (task.IsCanceled)
        {
            statusText = "Canceled";
        }
        else
        {
            authCode = task.Result.AuthCode;
            idToken = task.Result.IdToken;
            email = task.Result.Email;
            statusText = "Welcome: " + task.Result.DisplayName + "!\n" + idToken + " = idToken\n" + email + " = email";
        }
    }

    public void OnGoogleSignOut()
    {
        GoogleSignIn.DefaultInstance.SignOut();
        statusText = "Signed out of Google";
    }

    //purchasing 

    public void InitializePurchasing()
    {
        // If we have already connected to Purchasing ...
        if (IsInitialized())
        {
            // ... we are done here.
            return;
        }

        // Create a builder, first passing in a suite of Unity provided stores.
        var configurationBuilder = ConfigurationBuilder.Instance(Google.Play.Billing.GooglePlayStoreModule.Instance()); //For Google purchasing specifically
        //var configurationBuilder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Add a product to sell / restore by way of its identifier, associating the general identifier
        // with its store-specific identifiers.
        configurationBuilder.AddProduct(kProductIDConsumable, ProductType.Consumable);

        // Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
        // and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
        UnityPurchasing.Initialize(this, configurationBuilder);
    }

    private bool IsInitialized()
    {
        // Only say we are initialized if both the Purchasing references are set.
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    public void OnGooglePurchase()
    {
        // Buy the consumable product using its general identifier. Expect a response either 
        // through ProcessPurchase or OnPurchaseFailed asynchronously.
        BuyProductID(kProductIDConsumable);
    }

    public void OnVerifyPurchase()
    {
        //if (amazonReceiptId != null)
        //    BCLogs.GetComponent<Text>().text += "amazonReceiptId = " + amazonReceiptId;
        //else
        //    BCLogs.GetComponent<Text>().text += "\namazonReceiptId NULL";
        //if (amazonUserId != null)
        //    BCLogs.GetComponent<Text>().text += "\namazonUserId = " + amazonUserId;
        //else
        //    BCLogs.GetComponent<Text>().text += "\namazonUserId NULL";

        //string data = "{\"receiptId\":\"" + amazonReceiptId + "\",\"userId\":\"" + amazonUserId + "\"}";
        //Status.GetComponent<Text>().text += "\ndata" + data;

        Dictionary<string, object> receiptData = new Dictionary<string, object>();
        receiptData.Add("productId", kProductIDConsumable);
        receiptData.Add("orderId", "");
        receiptData.Add("token", "");
        receiptData.Add("developerPayload", "");

        string receiptDataString = JsonWriter.Serialize(receiptData);

        BCConfig._bc.AppStoreService.VerifyPurchase("googlePlay", receiptDataString, OnSuccess_VerifyPurchase, OnError_VerifyPurchase);
    }

    public void OnSuccess_VerifyPurchase(string responseData, object cbObject)
    {
        statusText = "Verified Purchase!\n" + responseData;
    }

    public void OnError_VerifyPurchase(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        statusText = "Failed to Verify Purchase...\n" + statusMessage + "\n" + reasonCode;
    }


    void BuyProductID(string productId)
    {
        // If Purchasing has been initialized ...
        if (IsInitialized())
        {
            // ... look up the Product reference with the general product identifier and the Purchasing 
            // system's products collection.
            Product product = m_StoreController.products.WithID(productId);

            // If the look up found a product for this device's store and that product is ready to be sold ... 
            if (product != null && product.availableToPurchase)
            {
                Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
                // asynchronously.
                m_StoreController.InitiatePurchase(product);
            }
            // Otherwise ...
            else
            {
                // ... report the product look-up failure situation  
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        // Otherwise ...
        else
        {
            // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
            // retrying initiailization.
            Debug.Log("BuyProductID FAIL. Not initialized.");
        }
    }

    public void OnShowGoogleStats()
    {
        statusText = "STORE: " + store +"\nPAYLOAD: " + payload + "\nJSON: " + gpJson + "\nSIGNATURE: " + gpSig;
    }


















    //  
    // --- IStoreListener callbacks
    //

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        // Purchasing has succeeded initializing. Collect our Purchasing references.
        statusText = "OnInitialized: Google Store PASS";
        //Debug.Log("OnInitialized: PASS");

        // Overall Purchasing system, configured with products for this application.
        m_StoreController = controller;
        // Store specific subsystem, for accessing device-specific store features.
        m_StoreExtensionProvider = extensions;
    }


    public void OnInitializeFailed(InitializationFailureReason error)
    {
        // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
        statusText = "OnInitializeFailed InitializationFailureReason:" + error;
        //statusText = "blah bala";
        //Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }


    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        // A consumable product has been purchased by this user.
        if (String.Equals(args.purchasedProduct.definition.id, kProductIDConsumable, StringComparison.Ordinal))
        {
            //Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            statusText = "ProcessPurchase: PASS. Product: " + args.purchasedProduct.definition.id;
        }
        else
        {
            //Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            statusText = "ProcessPurchase: FAIL. Unrecognized product: " + args.purchasedProduct.definition.id;
        }

        wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(args.purchasedProduct.receipt);
        store = (string)wrapper["Store"];
        payload = (string)wrapper["Payload"];
        gpDetails = (Dictionary<string, object>)MiniJson.JsonDecode(payload);
        gpJson = (string)gpDetails["json"];
        gpSig = (string)gpDetails["signature"];

        // Return a flag indicating whether this product has completely been received, or if the application needs 
        // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
        // saving purchased products to the cloud, and when that save is delayed. 
        return PurchaseProcessingResult.Complete;
    }


    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
        // this reason with the user to guide their troubleshooting actions.
        //Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        statusText = "OnPurchaseFailed: FAIL. Product: " + product.definition.storeSpecificId + ", PurchaseFailureReason: " + failureReason;
    }
}
