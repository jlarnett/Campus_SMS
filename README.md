# Campus_SMS

## **Project Overview**

Campus\_SMS is an **AI-powered SMS chatbot** designed to provide students with academic support outside standard faculty hours. It integrates **Twilio for SMS communication** and **AI (Open AI GPT) for generating responses**. If the AI cannot confidently answer a query, it offers **faculty escalation** via email and a web portal.

## **How It Works**

1. **A student sends an SMS inquiry** to the system.
2. **AI processes the query** using preloaded syllabus and academic data.
3. If the AI is **confident**, it responds instantly. UI
4. If the AI is **not confident**, it offers **faculty escalation**.
5. **Faculty members receive an email** with the student’s message and respond through a **web portal**.
6. **Admins and faculty** can update FAQs, manage escalations, and send announcements via SMS.

## **Key Features**

- **AI-Powered SMS Assistance**: Uses **Open AI GPT** for automated responses.
- **Twilio SMS Integration**: Handles student interactions through text messaging.
- **Faculty Escalation System**: Emails unanswered queries to faculty for manual responses.
- **Admin Dashboard**: Faculty/admins manage AI responses, escalations, and send SMS announcements.
- **Secure Database (SQL Server)**: Stores inquiry history, escalations, and AI response logs.
- **Cloud Deployment (Azure)**: Ensures scalability and availability.

## **Database Structure**

- **SMSInteraction Table**: Stores all student inquiries and AI responses.
- **Users Table**: Stores faculty, admin, and student user data.
- **Escalations Table**: Tracks all faculty escalations and responses.
- **Announcements Table**: Logs mass SMS messages sent by faculty.

## **System Workflow**

- **Student SMS → AI Query Handling** (Preloaded FAQ Database)
- **Confidence Check**: AI determines if it can answer accurately.
  - If **Confident** → Sends AI-generated response via Twilio.
  - If **Not Confident** → AI triggers **faculty escalation**.
- **Faculty Escalation**:
  - Faculty receives **email notification**.
  - Faculty logs into the **web portal** and responds.
- **Admin Portal Functions**:
  - Manage users (faculty & students).
  - Monitor AI response accuracy & update FAQs.
  - Send **bulk SMS announcements**.

## **Installation & Setup**

### **1. Clone the Repository**

```bash
git clone https://github.com/jlarnett/Campus_SMS.git
cd Campus_SMS
```

### **2. Install Dotnet Nuget Dependencies**

```powershell
dotnet restore
```

### **3. Configure API Keys**

API keys are currently stored via Visual Studio User Secrets or held in Azure Key Vault
If cloning repo, you may have to change the appsetttings.json value for "KeyVaultName" to pull keys from your azure key store
If stored correctly, the keys should be accessed via calls like so builder.Configuration["OpenAI:RobertAPIKey"]
```
Auth0--CallbackPath=your_auth0_callbackUrlPath
Auth0--ClientId=your_auth0_clientId
Auth0--ClientSecret=your_auth0_clientSecret
Auth0--Domain=your_auth0_domain
OpenAI--RobertAPIKey=your_openAI_key
Twilio--AccountSID=your_account_sid
Twilio--AuthToken=your_twilio_authtoken
Twilio--FromPhoneNumber=your_twilio_from_phoneNumber
```

### **4. Configure Github Action Repository Secrets**
For the github actions to work it will be required to modify/add the CLIENTID, TENANTID, SUBSCRIPTIONID secrets to your github repository
![image](https://github.com/user-attachments/assets/ba65e602-8bf0-43cb-9485-dfc5b37c68cd)

**Filename: master_campussms.yml will also require updating to reflect your Github Action Repository Secrets & Azure**
```yaml
client-id: ${{ secrets.AZUREAPPSERVICE_CLIENTID_D519706307304135AA6D88CFD61FDFF1 }}
tenant-id: ${{ secrets.AZUREAPPSERVICE_TENANTID_E31E3149FC4C48848DD609C3BDDC5CF7 }}
subscription-id: ${{ secrets.AZUREAPPSERVICE_SUBSCRIPTIONID_19A0F0BC1FDB4DB3AECB88429F84DD42 }}
```


### **5. Run the Application**

```powershell
dotnet run   #Starts both backend and frontend components
```

### **Lastly Cloud Considerations**
If using Azure it may be required to setup managed Identity for the Campus SMS App Service
to integrate with DB successfully. Owner, or reader/writer permissions will have to be given to the app service on the DB Server IAM (Access Control) roles selections
Otherwise you will run into permission problems

![image](https://github.com/user-attachments/assets/f4e08809-492b-440e-a4c4-b08af9e23b1b)

## **Future Enhancements**

- **Admin Dashboard Enhancements**: Improved UI for managing escalations.
- **Message Analytics**: Tracking student queries and AI response accuracy.
- **Expanded AI Training**: Improve AI responses through faculty input.

## **Contributors**

- **Robert Mahoney** - AI & Twilio Setup
- **Andrew Holmes** - Database & Backend
- **Gage Cook** - Database & Backend
- **Johnny Arnett** - Frontend & GitHub Management
- **Samuel Hornick** - Frontend Development

## **Project Links**

- **GitHub Repository**: [Campus\_SMS Repo](https://github.com/jlarnett/Campus_SMS)
- **Live Demo (If Available)**: [CampusSMS Web Portal](https://campussms-bbfyaza8gkecgpd6.eastus-01.azurewebsites.net/Identity/Account/Login)


