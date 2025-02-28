# Campus_SMS

## **Project Overview**
Campus_SMS is an **AI-powered SMS chatbot** designed to provide students with academic support outside standard faculty hours. It integrates **Twilio for SMS communication** and **AI (Copilot/Ollama) for generating responses**. If the AI cannot confidently answer a query, it offers **faculty escalation** via email and a web portal.

## **How It Works**
1. **A student sends an SMS inquiry** to the system.
2. **AI processes the query** using preloaded syllabus and academic data.
3. If the AI is **confident**, it responds instantly.
4. If the AI is **not confident**, it offers **faculty escalation**.
5. **Faculty members receive an email** with the student’s message and respond through a **web portal**.
6. **Admins and faculty** can update FAQs, manage escalations, and send announcements via SMS.

## **Key Features**
- **AI-Powered SMS Assistance**: Uses **Copilot/Ollama** for automated responses.
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
### **2. Install Dependencies**
```bash
npm install   # If using Node.js frontend
```
### **3. Configure API Keys**
Create a `.env` file with:
```
TWILIO_ACCOUNT_SID=your_account_sid
TWILIO_AUTH_TOKEN=your_auth_token
AI_API_KEY=your_ai_api_key
DATABASE_URL=your_database_url
```
### **4. Run the Application**
```bash
npm start    # For frontend
```
```bash
dotnet run   # For backend
```

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
- **GitHub Repository**: [Campus_SMS Repo](https://github.com/jlarnett/Campus_SMS)
- **Live Demo (If Available)**: [CampusSMS Web Portal](https://campussms-bbfyaza8gkecgpd6.eastus-01.azurewebsites.net/Identity/Account/Login)

