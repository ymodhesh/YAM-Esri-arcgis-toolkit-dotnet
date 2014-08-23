﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Controls;

#if !WINDOWS_PHONE_APP
#error "Intended for WinPhone only"
#endif

namespace Esri.ArcGISRuntime.Toolkit.Security
{
	/// <summary>
	/// WinPhone component that handles the authorization errors returned by the requests to the ArcGIS resources.
	/// <para>
	/// This component is designed to work with the <see cref="IdentityManager" />.
	/// It can be initialized with code like:
	/// <code>
	/// IdentityManager.Current.ChallengeHandler = new Esri.ArcGISRuntime.Toolkit.Security.WinPhoneChallengeHandler();
	/// </code>
	/// </para>
	/// <para/>
	/// Optionally, depending on the <see cref="AllowSaveCredentials"/> value, the credentials are cached in the <see cref="PasswordVault"/> in a secure manner.
	/// The <see cref="PasswordVault"/> roams credentials to other Windows8 systems.
	/// </summary>
	public class WinPhoneChallengeHandler // : IChallengeHandler to add when checked in
	{
		private bool _allowSaveCredentials;
		private bool _areCredentialsRestored;
		private readonly SignInDialog _signInDialog;

		/// <summary>
		/// Initializes a new instance of the <see cref="WinPhoneChallengeHandler"/> class.
		/// </summary>
		public WinPhoneChallengeHandler()
		{
			_signInDialog = new SignInDialog();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WinPhoneChallengeHandler"/> class.
		/// </summary>
		/// <param name="signInDialog">The underlying SignInDialog that will displayed inside a ContentDialog.</param>
		/// <exception cref="System.ArgumentNullException">signInDialog</exception>
		public WinPhoneChallengeHandler(SignInDialog signInDialog)
		{
			if (signInDialog == null)
				throw new ArgumentNullException("signInDialog");
			_signInDialog = signInDialog;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the credentials can be saved in the credential locker.
		/// <para/>
		/// The first time AllowSaveCredentials is set to true, the cached credentials are added to the <see cref="IdentityManager.AddCredential">IdentityManager</see>
		/// </summary>
		/// <remark>The default value is false.</remark>
		public bool AllowSaveCredentials
		{
			get { return _allowSaveCredentials; }
			set
			{
				if (_allowSaveCredentials != value)
				{
					_allowSaveCredentials = value;
					if (_allowSaveCredentials && !_areCredentialsRestored)
					{
						// The first time AllowSaveCredentials is set to true, add the cached credentials to IM
						_areCredentialsRestored = true;
						foreach (var crd in RetrieveAllSavedCredentials())
							IdentityManager.Current.AddCredential(crd);
					}
				}
			}
		}


		/// <summary>
		/// Gets or sets the option that specifies the initial state of the dialog's Save Credential check
		//     box. The default value is clear (unchecked).
		/// </summary>
		public CredentialSaveOption CredentialSaveOption { get; set; }

		/// <summary>
		/// Clears all ArcGISRuntime credentials from the Credential Locker.
		/// </summary>
		public void ClearCredentialsCache()
		{
			CredentialManager.RemoveAllCredentials();
		}

		/// <summary>
		/// Retrieves all ArcGISRuntime credentials stored in the Credential Locker.
		/// </summary>
		/// <returns></returns>
		internal IEnumerable<Credential> RetrieveAllSavedCredentials()
		{
			return CredentialManager.RetrieveAll();
		}

		/// <summary>
		/// Challenge for getting the credential allowing to access to the specified ArcGIS resource.
		/// </summary>
		/// <param name="credentialRequestInfo">Information about the ArcGIS resource that needs a credential for getting access to.</param>
		/// <returns>a Task object with <see cref="Credential"/> upon successful completion. 
		/// Otherwise, the Task.Exception is set.</returns>
		public virtual Task<Credential> CreateCredentialAsync(CredentialRequestInfo credentialRequestInfo)
		{
			if (credentialRequestInfo == null)
				throw new ArgumentNullException("credentialRequestInfo");

			var serverInfo = IdentityManager.Current.FindServerInfo(credentialRequestInfo.ServiceUri);

			// Check if we need to use OAuth for login.
			// In this case we don't have to display the SignInDialog by ourself but we have to go through the OAuth authorization page
			bool isOauth = false;
			if (serverInfo != null && credentialRequestInfo.AuthenticationType == AuthenticationType.Token)
			{
				if (serverInfo.TokenAuthenticationType != TokenAuthenticationType.ArcGISToken)
				{
					isOauth = true; // portal secured by OAuth
				}
				else if (!string.IsNullOrEmpty(serverInfo.OwningSystemUri))
				{
					// server federated to OAuth portal?
					// Check if the portal uses OAuth
					isOauth = IdentityManager.Current.ServerInfos.Any(s => SameOwningSystem(s, serverInfo) && s.TokenAuthenticationType != TokenAuthenticationType.ArcGISToken);
				}
			}

			if (isOauth)
				// OAuth case --> call GenerateCredentialAsync (that will throw an exception if the OAuthAuthorize component is not set)
				return OAuthTokenUI(credentialRequestInfo);



			// OAuth Authentication
			return _signInDialog.CreateCredentialAsync(credentialRequestInfo);
		}

		private async Task<Credential> OAuthTokenUI(CredentialRequestInfo credentialRequestInfo)
		{
			TokenCredential credential = await IdentityManager.Current.GenerateCredentialAsync(credentialRequestInfo.ServiceUri, credentialRequestInfo.GenerateTokenOptions);
			if (AllowSaveCredentials)
				CredentialManager.AddCredential(credential);
			return credential;
		}


		private static bool SameOwningSystem(ServerInfo info1, ServerInfo info2)
		{
			string owningSystemUrl1 = info1.OwningSystemUri;
			string owningSystemUrl2 = info2.OwningSystemUri;
			if (owningSystemUrl1 == null || owningSystemUrl2 == null)
				return false;

			// test without taking care of the scheme
			owningSystemUrl1 = owningSystemUrl1.Replace("https:", "http:");
			owningSystemUrl2 = owningSystemUrl2.Replace("https:", "http:");
			return owningSystemUrl1 == owningSystemUrl2;
		}
	}
}
