// import { Injectable } from '@angular/core';
// import {
//   HttpInterceptor,
//   HttpRequest,
//   HttpHandler,
//   HttpEvent,
//   HttpResponse,
//   HttpErrorResponse,
// } from '@angular/common/http';
// import { Observable, throwError } from 'rxjs';
// import { catchError, switchMap } from 'rxjs/operators';
// import { AuthService } from './auth.service';
// import { Router } from '@angular/router';

// @Injectable()
// export class TokenInterceptor implements HttpInterceptor {
//   constructor(private router: Router, private authService: AuthService) {}

//   intercept(
//     request: HttpRequest<any>,
//     next: HttpHandler
//   ): Observable<HttpEvent<any>> {
//     return next.handle(request).pipe(
//       catchError((error) => {
//         // Check if the error is due to unauthorized access (401)
//         if (error instanceof HttpErrorResponse && error.status === 401) {
//           // Attempt to refresh the access token
//           return this.authService.refreshToken().pipe(
//             switchMap(() => {
//               // If token refresh is successful, retry the original request
//               request = request.clone({
//                 setHeaders: {
//                   Authorization: `Bearer ${this.authService.getAccessToken()}`,
//                 },
//               });
//               return next.handle(request);
//             }),
//             catchError((refreshTokenError) => {
//               // If token refresh fails, logout the user or handle the error accordingly
//               // For simplicity, let's assume we log out the user
//               this.authService.removeToken();
//               this.router.navigate(['/selfhosted']);

//               return throwError(() => refreshTokenError);
//             })
//           );
//         }
//         return throwError(() => error);
//       })
//     );
//   }

//   private addToken(request: HttpRequest<any>, token: string): HttpRequest<any> {
//     return request.clone({
//       setHeaders: {
//         Authorization: `Bearer ${token}`,
//       },
//     });
//   }
// }

// token-interceptor.service.ts

import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

@Injectable()
export class TokenInterceptor implements HttpInterceptor {
  constructor(private router: Router, private authService: AuthService) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    // Add the token to the request headers
    request = this.addTokenToRequest(request);

    return next.handle(request).pipe(
      catchError((error) => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          return this.handle401Error(request, next);
        } else {
          return throwError(error);
        }
      })
    );
  }

  private addTokenToRequest(request: HttpRequest<any>): HttpRequest<any> {
    const accessToken = this.authService.getAccessToken();
    if (accessToken) {
      return request.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
    }
    return request;
  }

  private handle401Error(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    // Attempt to refresh the access token
    return this.authService.refreshToken().pipe(
      switchMap(() => {
        // If token refresh is successful, retry the original request with the new token
        request = this.addTokenToRequest(request);
        return next.handle(request);
      }),
      catchError((refreshTokenError) => {
        // If token refresh fails, logout the user or handle the error accordingly
        // For simplicity, let's assume we log out the user
        this.authService.removeToken();
        this.router.navigate(['/selfhosted']);
        return throwError(() => refreshTokenError);
      })
    );
  }
}
