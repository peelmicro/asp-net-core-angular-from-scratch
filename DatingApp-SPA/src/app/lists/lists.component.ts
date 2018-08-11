import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { User } from '../_models/user';
import { Pagination, PaginatedResult } from '../_models/pagination';
import { UserService } from '../_services/user.service';
import { AlertifyService } from '../_services/alertify.service';
import { AuthService } from '../_services/auth.service';

@Component({
  selector: 'app-lists',
  templateUrl: './lists.component.html',
  styleUrls: ['./lists.component.css']
})
export class ListsComponent implements OnInit {
  users: User[];
  pagination: Pagination;
  likesParam: string;
  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private userService: UserService,
    private alertifyService: AlertifyService
  ) { }

  ngOnInit() {
    this.route.data.subscribe(data => {
      this.users = data['users'].result;
      this.pagination = data['users'].pagination;
    });
    this.likesParam = 'Likers';
  }

  pageChanged(event: any): void {
    this.pagination.currentPage = event.page;
    this.loadUsers();
  }

  loadUsers() {
    this.userService
      .getUsers(
        this.pagination.currentPage,
        this.pagination.itemsPerPage,
        null,
        this.likesParam
      )
      .subscribe(
        (result: PaginatedResult<User[]>) => {
          this.users = result.result;
          this.pagination = result.pagination;
        },
        error => this.alertifyService.error(error)
      );
  }

}
