// Load Fhir questionaire from path.
function LoadQuestionaireFromPath(file) {
    let reader = new FileReader();
    reader.readAsText(file);
    reader.onload = function () {
        LForms.Util.addFormToPage(reader.result, 'formContainer');
    };
    reader.onerror = function () {
        console.log(reader.error);
    };
}
