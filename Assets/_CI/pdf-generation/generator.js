
var markdownpdf = require("markdown-pdf")
var fs = require("fs")

var src = process.argv[2];
var trg = process.argv[3];

fs.exists(src, function(src_exists){
  
  if( src_exists )
  {
    fs.exists( trg, function( trg_exists )
    {
      
      if( trg_exists)
      {
        console.log( "Target file already exists and will removed: " +  trg);
        fs.unlink( trg );
      }
      
        fs.createReadStream( src ).pipe( markdownpdf() ).pipe( fs.createWriteStream( trg ));
    });
  }
  else
    console.error("File Not Found: " + src);
});
