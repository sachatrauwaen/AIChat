Opencontent is composed by Handlebars HTML templates based on JSON data defined in a json schema.

The root of opencontent templates is "/OpenContent/Templates"
The list of opencontent templates is the list of folders "/OpenContent/Templates"

structure of a template folder this files :
- data.json
- schema.json
- options.json
- template.hbs
- template.css
- template.js

template is just a example of the default template name.
but there can be multiple template files in a folder.

Exemple options.json:
{
  "fields": {
    "Columns": {
      "items": {
        "fields": {
          "Title": {
            "dependencies": {},
            "type": "text"
          },
          "Image": {
            "dependencies": {},
            "type": "image"
          },
          "Description": {
            "dependencies": {},
            "type": "ckeditor"
          }
        }
      }
    }
  }
}

There is only one schema.json in each template folder.
The HTML template should be written using Handlebars syntax.
The JSON schema defines the data structure that the HTML template will display.   
The template generates html fragments, not full html pages.
The css is in a file with the same name as the template file but with css extension.
The schema.json needs to contain a title for each property.
The options.json is used to generate the form in the admin interface.
The data.json is used to generate the data that the template will display.
The data.json will contain images with relative url .
The template.js is used to add some javascript code to the template.
The template.css is used to add some css to the template.
The template.hbs is the main template file that will be used to display the data.

Here is an exemple of a template folder:

/OpenContent/Templates/MyTemplate/
  - data.json
  - schema.json
  - options.json
  - template.hbs
  - template.css
  - template.js 

  This a the available handlebars helpers, do not use other helpers:

-  @index :   {{ @index}} Index of the Item inside an {{#each Items}} list
-  @first :   {{ @first}} is the item the first item inside an {{#each Items}} list
-  @last :   {{ @last}} is the item the last item inside an {{#each Items}} list

- odd :		{{#odd @index}} .... {{else}} ... {{/odd}}
- even :		{{#even @index}} .... {{else}} ... {{/even}}
- equal :		{{#equal Title "Text"}} .... {{else}} ... {{/equal}}
- multiply :		{{multiply SettingsColumns 2}}
- divide :		{{divide SettingsColumns 2}}
- add :		{{add @index 1}}
- substract :		{{substract Settings.Columns 1}}
- published :		{{published}}
- registerscript :		{{registerscript "js/script.js"}}
- registerstylesheet :		{{registerstylesheet "css/style.css"}}
- registerservicesframework :		{{registerservicesframework}}
- arrayindex :		{{arrayindex}}
- arraytranslate :		{{arraytranslate}}
- formatNumber :		{{formatNumber}}
- formatDateTime :		{{formatDateTime DateField "dd/MMM/yy" "nl-NL" }}
- ifand :	Yes	multiple variables to check		{{#ifand Title Summary}} .... {{else}} ... {{/ifand}}
- ifor :		{{#ifor Title Summary}} .... {{else}} ... {{/ifor}}
- convertHtmlToText :		{{convertHtmlToText Description}}
- replacenewline :		{{replacenewline Title "<br>"}}
- replace :		{{replace Title "x" "y"}}
- truncateWords :	No	text, maxCharacters, trailingText	string	{{truncateWords Description 50 "..."}}
- raw :		{{{{raw}}}}
- contains :		{{#contains Title "hello"}} .... {{else}} ... {{/contains}}
- prefixurl :		{{prefixurl url}}
