Below are two updated user stories that incorporate the **Email** and **Print** modal images as well as the **Figma** design link for the **Delivery | UK** project. Each story references the required design guidelines, layout, and visual elements.

---

## User Story 1: Print File Modal & Toolbar Button Integration

**Title:**  
Print File Modal & Toolbar Button Integration

**Description:**  
_As a user, I want to print a file directly from the chat interface so that I can quickly obtain a hard copy and track the progress of the print operation. When I click the Print button on the toolbar, a modal should appear to confirm the print action, show progress details, and allow me to configure certain print settings._

**Acceptance Criteria:**

1. **Toolbar Integration**
    
    - Extend the existing delivery toolbar with a **Print** button.
    - The button’s style matches the toolbar’s current design and adheres to the provided look/feel.
2. **Print Modal UI**
    
    - Clicking the **Print** button opens a modal dialog (as shown in the provided Print modal image).
    - The modal includes the following sections/controls:
        - **Document**: An option to include or exclude the LexisNexis logo (or any relevant branding element).
        - **Formatting**: Page numbering location (e.g., top-right, bottom-center, bottom-right).
        - **Print Specs**:
            - A toggle or dropdown to select the printer (e.g., “Attached printer”).
            - Any additional printing options needed, as shown in the design (e.g., single/double-sided, if applicable).
        - **Terms & Conditions**: A short text indicating “Distribution is subject to Terms & Conditions,” with a link or reference if needed.
    - The modal design aligns with **ABE** guidelines and the **Figma** design files.
3. **Print Flow & Feedback**
    
    - When the user confirms (clicks **Print**), show progress or status updates in the modal (e.g., “Preparing print job,” “Printing in progress,” “Print complete”).
    - Handle potential errors gracefully (e.g., printer not available), displaying an error message in the modal.
4. **Mocked Implementation**
    
    - The file retrieval and print requests are mocked for now; real backend integration will happen in a future story.
    - The modal should still display the simulated progress/status updates.
5. **Implementation Notes**
    
    - This feature is part of the AI-specific code for the UK, with the intent to migrate it to Global AI code in the future.
    - Ensure the addition of the Print button does not interfere with existing toolbar functionality (e.g., Download).

---

## User Story 2: Send Email Modal & Toolbar Button Integration

**Title:**  
Send File via Email Modal & Toolbar Button Integration

**Description:**  
_As a user, I want to send a file via email directly from the chat interface so that I can quickly share files with others without leaving the application. When I click the Email button on the toolbar, a modal should appear to let me configure email details, confirm the sending process, and track the progress._

**Acceptance Criteria:**

1. **Toolbar Integration**
    
    - Extend the existing delivery toolbar with a **Send Email** button.
    - The button’s design matches the current toolbar styling and the visual references in the provided images.
2. **Email Modal UI**
    
    - Clicking the **Send Email** button opens a modal dialog (as shown in the provided Email modal image).
    - The modal includes the following sections/controls:
        - **To**: One or more recipient email address fields (including any validation or “Myself” option, if shown).
        - **Subject**: A field to specify the email subject.
        - **Description / Notes**: An optional text area to include additional information.
        - **Formatting**: Page numbering (if applicable, similar to Print).
        - **Email Specs**:
            - **Document Format** (e.g., PDF).
            - **File Name** field.
        - **Include**: Option to include the LexisNexis logo or other branding, if needed.
        - **Terms & Conditions**: A statement such as “Distribution is subject to Terms & Conditions.”
3. **Email Sending Flow & Feedback**
    
    - When the user confirms (clicks **Email**), show progress updates in the modal (e.g., “Sending…,” “Email sent successfully,” or error messages).
    - Validate the email address format before sending; display any validation errors clearly.
4. **Mocked Implementation**
    
    - The email request is mocked for now, with simulated success or error states.
    - Future stories will integrate real backend/email service.
5. **Implementation Notes**
    
    - This feature is part of the AI-specific code for the UK and will later be migrated to the Global AI code.
    - Ensure the addition of the Email button does not disrupt existing toolbar functionality (e.g., Download, Print).

---

### Additional Design References

- **Email Modal Image & Print Modal Image**  
    Refer to the screenshots provided for the exact layout and field arrangement.
- **Figma Link**  
    For detailed UI elements, spacing, colors, and overall design, consult the [Figma “Delivery | UK” file](https://www.figma.com/design/jGBaqvsJjMsga1eaMTDii5/Delivery-%7C-UK?node-id=4127-86316&t=nLvPX4kf6m6O7xGZ-4).
    - Ensure all design elements follow the **ABE** design system and match the Figma specifications.

These updated stories integrate the **Email** and **Print** modal images, as well as the **Figma** design link, ensuring that the new functionality and UI elements adhere to your organization’s style guidelines and technical requirements.