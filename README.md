# RaspSVD
This application demonstrates SVD usage for image compression. Choose any picture (like PNG), select ratio and compress the image.

![Open file](/SVD/Screenshots/Open.png)

## Functionality
### Loading images
Open any picture natively supported by Windows using system dialog.
![Open file](/SVD/Screenshots/Compressed.png)

### Compression
Compression is performed using SVD algorithm. It comes with a NuGet plugin Math.NET. Please note that high ratios might actually increase the size of the imge due to SVD's nature.

### Parallelism
This application will use up to 4 threads (RGBA) to perform the compression.
