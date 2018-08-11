import { Injectable } from '@angular/core';
import { Resolve, Router, ActivatedRouteSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';

import { User } from '../_models/user';
import { UserService } from '../_services/user.service';
import { AlertifyService } from '../_services/alertify.service';
import { catchError, map } from 'rxjs/operators';

@Injectable()
export class MemberDetailResolver implements Resolve<User> {
  constructor(
    private userService: UserService,
    private router: Router,
    private alertify: AlertifyService
  ) {}

  resolve(route: ActivatedRouteSnapshot): Observable<User> {
    return this.userService.getUser(+route.params['id']).pipe(
      map(user => {
        if (!user) {
          this.problem();
          return of(null);
        }
        return user;
      }),
      catchError(() => {
        this.problem();
        return of(null);
      })
    );
  }

  problem() {
    this.alertify.error('Problem retrieving data');
    this.router.navigate(['/members']);
  }
}
