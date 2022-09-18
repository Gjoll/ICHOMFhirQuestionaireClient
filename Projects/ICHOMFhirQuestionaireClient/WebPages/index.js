// Load Fhir questionaire.
function loadQuestionaire(data, patientData) {
    alert("Data Type " + typeof (data));
    alert("Patient Type " + typeof (patientData));
    LForms.Util.setFHIRContext(null,
        {
            patient: patientData
        }
    );

    LForms.Util.addFormToPage(data.questionaire, 'formContainer', '{prepopulate: true}');
}

// return Fhir questionaire data.
function saveQuestionaire(data) {
    // var noFormDefData = true;  // If this is true, the form definition data will not be returned along with the data.
    // var noEmptyValue = false;   //If this is true, items that have an empty value will be removed.
    // var noHiddenItem = false;   // If this is true, items that are hidden by skip logic will be removed.
    // return LForms.Util.getUserData('formContainer', noFormDefData, noEmptyValue, noHiddenItem);

    return LForms.Util.getFormFHIRData("QuestionnaireResponse", "R4", 'formContainer');
}
